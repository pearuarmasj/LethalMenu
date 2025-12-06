#include "pch.h"
#include "renderer.h"
#include "hooks.h"
#include "exception.h"
#include "gui/menu.h"
#include "features/features.h"
#include "utils/logger.h"

#include <imgui.h>
#include <imgui_impl_win32.h>
#include <imgui_impl_dx11.h>

// Forward declare ImGui Win32 handler
extern IMGUI_IMPL_API LRESULT ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

namespace core
{
    Renderer& Renderer::Get()
    {
        static Renderer instance;
        s_instance = &instance;
        return instance;
    }

    bool Renderer::Initialize()
    {
        if (m_initialized)
            return true;

        LOG_INFO("Initializing renderer...");

        if (!CreateDummySwapchain())
        {
            LOG_ERROR("Failed to create dummy swapchain");
            return false;
        }

        if (!HookPresent())
        {
            LOG_ERROR("Failed to hook Present");
            return false;
        }

        m_initialized = true;
        LOG_INFO("Renderer initialized");
        return true;
    }

    void Renderer::Shutdown()
    {
        if (!m_initialized)
            return;

        LOG_INFO("Shutting down renderer...");

        // Restore WndProc
        if (m_hwnd && m_originalWndProc)
        {
            SetWindowLongPtrW(m_hwnd, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(m_originalWndProc));
            m_originalWndProc = nullptr;
        }

        // Shutdown ImGui
        if (m_imguiInitialized)
        {
            ImGui_ImplDX11_Shutdown();
            ImGui_ImplWin32_Shutdown();
            ImGui::DestroyContext();
            m_imguiInitialized = false;
        }

        // Release render target
        if (m_renderTargetView)
        {
            m_renderTargetView->Release();
            m_renderTargetView = nullptr;
        }

        // Remove hooks (MinHook handles this)
        Hooks::Get().RemoveHook("Present");
        Hooks::Get().RemoveHook("ResizeBuffers");

        m_device = nullptr;
        m_context = nullptr;
        m_hwnd = nullptr;
        m_initialized = false;

        LOG_INFO("Renderer shutdown complete");
    }

    bool Renderer::CreateDummySwapchain()
    {
        // Create a dummy window for swapchain creation
        WNDCLASSEXW wc = {};
        wc.cbSize = sizeof(wc);
        wc.lpfnWndProc = DefWindowProcW;
        wc.hInstance = GetModuleHandleW(nullptr);
        wc.lpszClassName = L"DummyDX11Window";

        if (!RegisterClassExW(&wc))
        {
            // Class might already exist, continue anyway
        }

        HWND dummyHwnd = CreateWindowExW(
            0, wc.lpszClassName, L"Dummy",
            WS_OVERLAPPEDWINDOW,
            0, 0, 100, 100,
            nullptr, nullptr, wc.hInstance, nullptr
        );

        if (!dummyHwnd)
        {
            LOG_ERROR("Failed to create dummy window");
            return false;
        }

        DXGI_SWAP_CHAIN_DESC scd = {};
        scd.BufferCount = 1;
        scd.BufferDesc.Width = 2;
        scd.BufferDesc.Height = 2;
        scd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
        scd.BufferDesc.RefreshRate.Numerator = 60;
        scd.BufferDesc.RefreshRate.Denominator = 1;
        scd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
        scd.OutputWindow = dummyHwnd;
        scd.SampleDesc.Count = 1;
        scd.Windowed = TRUE;
        scd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;

        D3D_FEATURE_LEVEL featureLevel;
        IDXGISwapChain* swapChain = nullptr;
        ID3D11Device* device = nullptr;
        ID3D11DeviceContext* context = nullptr;

        HRESULT hr = D3D11CreateDeviceAndSwapChain(
            nullptr,
            D3D_DRIVER_TYPE_HARDWARE,
            nullptr,
            0,
            nullptr,
            0,
            D3D11_SDK_VERSION,
            &scd,
            &swapChain,
            &device,
            &featureLevel,
            &context
        );

        if (FAILED(hr))
        {
            LOG_ERROR("D3D11CreateDeviceAndSwapChain failed: 0x%X", hr);
            DestroyWindow(dummyHwnd);
            return false;
        }

        // Get vtable from swapchain
        void** vtable = *reinterpret_cast<void***>(swapChain);

        // Present is index 8, ResizeBuffers is index 13
        s_originalPresent = reinterpret_cast<PresentFn>(vtable[8]);
        s_originalResizeBuffers = reinterpret_cast<ResizeBuffersFn>(vtable[13]);

        LOG_INFO("Present vtable address: %p", vtable[8]);
        LOG_INFO("ResizeBuffers vtable address: %p", vtable[13]);

        // Cleanup dummy objects
        swapChain->Release();
        device->Release();
        context->Release();
        DestroyWindow(dummyHwnd);

        return true;
    }

    bool Renderer::HookPresent()
    {
        auto& hooks = Hooks::Get();

        if (!hooks.AddHook("Present",
            reinterpret_cast<void*>(s_originalPresent),
            reinterpret_cast<void*>(&HookedPresent),
            reinterpret_cast<void**>(&s_originalPresent)))
        {
            LOG_ERROR("Failed to hook Present");
            return false;
        }

        if (!hooks.AddHook("ResizeBuffers",
            reinterpret_cast<void*>(s_originalResizeBuffers),
            reinterpret_cast<void*>(&HookedResizeBuffers),
            reinterpret_cast<void**>(&s_originalResizeBuffers)))
        {
            LOG_ERROR("Failed to hook ResizeBuffers");
            return false;
        }

        return true;
    }

    void Renderer::InitImGui(IDXGISwapChain* swapChain)
    {
        if (m_imguiInitialized)
            return;

        LOG_INFO("Initializing ImGui...");

        // Get device and context from swapchain
        if (FAILED(swapChain->GetDevice(__uuidof(ID3D11Device), reinterpret_cast<void**>(&m_device))))
        {
            LOG_ERROR("Failed to get D3D11 device from swapchain");
            return;
        }

        m_device->GetImmediateContext(&m_context);

        // Get window handle
        DXGI_SWAP_CHAIN_DESC desc;
        swapChain->GetDesc(&desc);
        m_hwnd = desc.OutputWindow;

        // Create render target view
        ID3D11Texture2D* backBuffer = nullptr;
        if (SUCCEEDED(swapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), reinterpret_cast<void**>(&backBuffer))))
        {
            m_device->CreateRenderTargetView(backBuffer, nullptr, &m_renderTargetView);
            backBuffer->Release();
        }

        // Setup ImGui
        IMGUI_CHECKVERSION();
        ImGui::CreateContext();

        ImGuiIO& io = ImGui::GetIO();
        io.ConfigFlags |= ImGuiConfigFlags_NavEnableKeyboard;
        io.IniFilename = nullptr; // Don't save settings

        // Style
        ImGui::StyleColorsDark();
        ImGuiStyle& style = ImGui::GetStyle();
        style.WindowRounding = 5.0f;
        style.FrameRounding = 3.0f;

        // Initialize platform/renderer backends
        ImGui_ImplWin32_Init(m_hwnd);
        ImGui_ImplDX11_Init(m_device, m_context);

        // Hook WndProc for input
        m_originalWndProc = reinterpret_cast<WNDPROC>(
            SetWindowLongPtrW(m_hwnd, GWLP_WNDPROC, reinterpret_cast<LONG_PTR>(&HookedWndProc))
        );

        m_imguiInitialized = true;
        LOG_INFO("ImGui initialized");
    }

    HRESULT WINAPI Renderer::HookedPresent(IDXGISwapChain* swapChain, UINT syncInterval, UINT flags)
    {
        auto& renderer = Renderer::Get();

        __try
        {
            // Initialize ImGui on first Present call
            if (!renderer.m_imguiInitialized)
            {
                renderer.InitImGui(swapChain);
            }

            // Render our stuff
            if (renderer.m_imguiInitialized && renderer.m_renderTargetView)
            {
                ImGui_ImplDX11_NewFrame();
                ImGui_ImplWin32_NewFrame();
                ImGui::NewFrame();

                // Render menu with protection
                __try
                {
                    gui::Menu::Get().Render();
                }
                __except (EXCEPTION_EXECUTE_HANDLER)
                {
                    // Menu render failed, log but continue
                    static bool logged = false;
                    if (!logged)
                    {
                        LOG_ERROR("Exception in Menu::Render() - code: 0x%X", GetExceptionCode());
                        logged = true;
                    }
                }

                // Update features (god mode, infinite stamina, etc.)
                __try
                {
                    features::PlayerFeatures::Get().Update();
                    features::MiscFeatures::Get().Update();
                }
                __except (EXCEPTION_EXECUTE_HANDLER)
                {
                    static bool logged = false;
                    if (!logged)
                    {
                        LOG_ERROR("Exception in Features::Update() - code: 0x%X", GetExceptionCode());
                        logged = true;
                    }
                }

                ImGui::Render();

                renderer.m_context->OMSetRenderTargets(1, &renderer.m_renderTargetView, nullptr);
                ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());
            }
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            static bool logged = false;
            if (!logged)
            {
                LOG_ERROR("Exception in HookedPresent - code: 0x%X", GetExceptionCode());
                logged = true;
            }
        }

        return s_originalPresent(swapChain, syncInterval, flags);
    }

    HRESULT WINAPI Renderer::HookedResizeBuffers(IDXGISwapChain* swapChain, UINT bufferCount, UINT width, UINT height, DXGI_FORMAT newFormat, UINT swapChainFlags)
    {
        auto& renderer = Renderer::Get();
        HRESULT hr = E_FAIL;

        __try
        {
            // Release render target before resize
            if (renderer.m_renderTargetView)
            {
                renderer.m_renderTargetView->Release();
                renderer.m_renderTargetView = nullptr;
            }

            // Call original
            hr = s_originalResizeBuffers(swapChain, bufferCount, width, height, newFormat, swapChainFlags);

            // Recreate render target
            if (SUCCEEDED(hr) && renderer.m_device)
            {
                ID3D11Texture2D* backBuffer = nullptr;
                if (SUCCEEDED(swapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), reinterpret_cast<void**>(&backBuffer))))
                {
                    renderer.m_device->CreateRenderTargetView(backBuffer, nullptr, &renderer.m_renderTargetView);
                    backBuffer->Release();
                }
            }
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            LOG_ERROR("Exception in HookedResizeBuffers - code: 0x%X", GetExceptionCode());
            // Try to call original anyway
            hr = s_originalResizeBuffers(swapChain, bufferCount, width, height, newFormat, swapChainFlags);
        }

        return hr;
    }

    LRESULT WINAPI Renderer::HookedWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
    {
        auto& renderer = Renderer::Get();

        __try
        {
            // Toggle menu with INSERT key
            if (msg == WM_KEYDOWN && wParam == VK_INSERT)
            {
                gui::Menu::Get().Toggle();
                return 0;
            }

            // Let ImGui handle input when menu is open
            if (renderer.m_imguiInitialized)
            {
                if (ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam))
                    return 0;

                // Block game input when menu is open
                if (gui::Menu::Get().IsOpen())
                {
                    switch (msg)
                    {
                    case WM_MOUSEMOVE:
                    case WM_LBUTTONDOWN:
                    case WM_LBUTTONUP:
                    case WM_RBUTTONDOWN:
                    case WM_RBUTTONUP:
                    case WM_MBUTTONDOWN:
                    case WM_MBUTTONUP:
                    case WM_MOUSEWHEEL:
                    case WM_KEYDOWN:
                    case WM_KEYUP:
                    case WM_CHAR:
                        return 0;
                    }
                }
            }
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            static bool logged = false;
            if (!logged)
            {
                LOG_ERROR("Exception in HookedWndProc - code: 0x%X", GetExceptionCode());
                logged = true;
            }
        }

        return CallWindowProcW(renderer.m_originalWndProc, hWnd, msg, wParam, lParam);
    }
}
