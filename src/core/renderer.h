#pragma once

#include <d3d11.h>
#include <dxgi.h>

namespace core
{
    class Renderer
    {
    public:
        static Renderer& Get();

        bool Initialize();
        void Shutdown();

        bool IsInitialized() const { return m_initialized; }

    private:
        Renderer() = default;
        ~Renderer() = default;

        Renderer(const Renderer&) = delete;
        Renderer& operator=(const Renderer&) = delete;

        bool CreateDummySwapchain();
        bool HookPresent();
        void InitImGui(IDXGISwapChain* swapChain);

        // Hook callbacks
        static HRESULT WINAPI HookedPresent(IDXGISwapChain* swapChain, UINT syncInterval, UINT flags);
        static HRESULT WINAPI HookedResizeBuffers(IDXGISwapChain* swapChain, UINT bufferCount, UINT width, UINT height, DXGI_FORMAT newFormat, UINT swapChainFlags);
        static LRESULT WINAPI HookedWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);

        bool m_initialized = false;
        bool m_imguiInitialized = false;

        // D3D11 objects
        ID3D11Device* m_device = nullptr;
        ID3D11DeviceContext* m_context = nullptr;
        ID3D11RenderTargetView* m_renderTargetView = nullptr;

        // Window
        HWND m_hwnd = nullptr;
        WNDPROC m_originalWndProc = nullptr;

        // Original functions
        using PresentFn = HRESULT(WINAPI*)(IDXGISwapChain*, UINT, UINT);
        using ResizeBuffersFn = HRESULT(WINAPI*)(IDXGISwapChain*, UINT, UINT, UINT, DXGI_FORMAT, UINT);

        static inline PresentFn s_originalPresent = nullptr;
        static inline ResizeBuffersFn s_originalResizeBuffers = nullptr;
        static inline Renderer* s_instance = nullptr;
    };
}
