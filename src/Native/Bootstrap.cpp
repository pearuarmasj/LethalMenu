// Native bootstrap DLL for injecting C# mod into Unity/Mono games
// This is loaded by standard Windows DLL injectors and calls into Mono runtime

#define WIN32_LEAN_AND_MEAN
#include <Windows.h>
#include <cstdio>
#include <string>

// Mono types
typedef void* MonoDomain;
typedef void* MonoAssembly;
typedef void* MonoImage;
typedef void* MonoClass;
typedef void* MonoMethod;
typedef void* MonoObject;
typedef void* MonoString;
typedef void* MonoThread;

// Mono function signatures
typedef MonoDomain* (__cdecl* mono_get_root_domain_t)();
typedef MonoThread* (__cdecl* mono_thread_attach_t)(MonoDomain* domain);
typedef MonoAssembly* (__cdecl* mono_domain_assembly_open_t)(MonoDomain* domain, const char* name);
typedef MonoImage* (__cdecl* mono_assembly_get_image_t)(MonoAssembly* assembly);
typedef MonoClass* (__cdecl* mono_class_from_name_t)(MonoImage* image, const char* name_space, const char* name);
typedef MonoMethod* (__cdecl* mono_class_get_method_from_name_t)(MonoClass* klass, const char* name, int param_count);
typedef MonoObject* (__cdecl* mono_runtime_invoke_t)(MonoMethod* method, void* obj, void** params, MonoObject** exc);

// Mono function pointers
mono_get_root_domain_t mono_get_root_domain = nullptr;
mono_thread_attach_t mono_thread_attach = nullptr;
mono_domain_assembly_open_t mono_domain_assembly_open = nullptr;
mono_assembly_get_image_t mono_assembly_get_image = nullptr;
mono_class_from_name_t mono_class_from_name = nullptr;
mono_class_get_method_from_name_t mono_class_get_method_from_name = nullptr;
mono_runtime_invoke_t mono_runtime_invoke = nullptr;

// Log file for debugging
FILE* g_logFile = nullptr;

void Log(const char* fmt, ...)
{
    if (!g_logFile)
    {
        fopen_s(&g_logFile, "C:\\LethalMenuNative.log", "w");
    }

    if (g_logFile)
    {
        va_list args;
        va_start(args, fmt);
        vfprintf(g_logFile, fmt, args);
        fprintf(g_logFile, "\n");
        fflush(g_logFile);
        va_end(args);
    }
}

bool LoadMonoFunctions()
{
    HMODULE mono = GetModuleHandleA("mono-2.0-bdwgc.dll");
    if (!mono)
    {
        mono = GetModuleHandleA("mono.dll");
    }
    if (!mono)
    {
        Log("ERROR: Could not find mono DLL");
        return false;
    }

    Log("Found mono at: 0x%p", mono);

    mono_get_root_domain = (mono_get_root_domain_t)GetProcAddress(mono, "mono_get_root_domain");
    mono_thread_attach = (mono_thread_attach_t)GetProcAddress(mono, "mono_thread_attach");
    mono_domain_assembly_open = (mono_domain_assembly_open_t)GetProcAddress(mono, "mono_domain_assembly_open");
    mono_assembly_get_image = (mono_assembly_get_image_t)GetProcAddress(mono, "mono_assembly_get_image");
    mono_class_from_name = (mono_class_from_name_t)GetProcAddress(mono, "mono_class_from_name");
    mono_class_get_method_from_name = (mono_class_get_method_from_name_t)GetProcAddress(mono, "mono_class_get_method_from_name");
    mono_runtime_invoke = (mono_runtime_invoke_t)GetProcAddress(mono, "mono_runtime_invoke");

    if (!mono_get_root_domain || !mono_thread_attach || !mono_domain_assembly_open ||
        !mono_assembly_get_image || !mono_class_from_name || !mono_class_get_method_from_name ||
        !mono_runtime_invoke)
    {
        Log("ERROR: Failed to get mono functions");
        Log("  mono_get_root_domain: 0x%p", mono_get_root_domain);
        Log("  mono_thread_attach: 0x%p", mono_thread_attach);
        Log("  mono_domain_assembly_open: 0x%p", mono_domain_assembly_open);
        Log("  mono_assembly_get_image: 0x%p", mono_assembly_get_image);
        Log("  mono_class_from_name: 0x%p", mono_class_from_name);
        Log("  mono_class_get_method_from_name: 0x%p", mono_class_get_method_from_name);
        Log("  mono_runtime_invoke: 0x%p", mono_runtime_invoke);
        return false;
    }

    Log("All mono functions loaded successfully");
    return true;
}

void InjectManagedDll()
{
    Log("Starting managed DLL injection...");

    // Get root domain
    MonoDomain* domain = mono_get_root_domain();
    if (!domain)
    {
        Log("ERROR: mono_get_root_domain returned null");
        return;
    }
    Log("Got root domain: 0x%p", domain);

    // Attach to the domain's thread
    MonoThread* thread = mono_thread_attach(domain);
    Log("Attached to thread: 0x%p", thread);

    // Get path to managed DLL (same directory as this native DLL)
    char dllPath[MAX_PATH];
    HMODULE hModule = nullptr;
    GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
        (LPCSTR)&InjectManagedDll, &hModule);
    GetModuleFileNameA(hModule, dllPath, MAX_PATH);

    // Replace native DLL name with managed DLL name
    std::string path(dllPath);
    size_t lastSlash = path.find_last_of("\\/");
    if (lastSlash != std::string::npos)
    {
        path = path.substr(0, lastSlash + 1);
    }
    path += "LethalMenu.dll";

    Log("Loading managed assembly from: %s", path.c_str());

    // Load the managed assembly
    MonoAssembly* assembly = mono_domain_assembly_open(domain, path.c_str());
    if (!assembly)
    {
        Log("ERROR: Failed to load assembly");
        return;
    }
    Log("Loaded assembly: 0x%p", assembly);

    // Get the image
    MonoImage* image = mono_assembly_get_image(assembly);
    if (!image)
    {
        Log("ERROR: Failed to get assembly image");
        return;
    }
    Log("Got image: 0x%p", image);

    // Find the Loader class
    MonoClass* loaderClass = mono_class_from_name(image, "LethalMenu", "Loader");
    if (!loaderClass)
    {
        Log("ERROR: Failed to find Loader class");
        return;
    }
    Log("Found Loader class: 0x%p", loaderClass);

    // Find the Load method
    MonoMethod* loadMethod = mono_class_get_method_from_name(loaderClass, "Load", 0);
    if (!loadMethod)
    {
        Log("ERROR: Failed to find Load method");
        return;
    }
    Log("Found Load method: 0x%p", loadMethod);

    // Invoke the Load method
    Log("Invoking Load method...");
    MonoObject* exception = nullptr;
    mono_runtime_invoke(loadMethod, nullptr, nullptr, &exception);

    if (exception)
    {
        Log("ERROR: Exception thrown during Load()");
    }
    else
    {
        Log("Load() completed successfully!");
    }
}

DWORD WINAPI MainThread(LPVOID param)
{
    Log("===========================================");
    Log("LethalMenu Native Bootstrap");
    Log("===========================================");

    // Wait a bit for game to fully initialize
    Sleep(1000);

    if (!LoadMonoFunctions())
    {
        Log("Failed to load mono functions, aborting");
        return 1;
    }

    InjectManagedDll();

    Log("Native bootstrap complete");
    return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID reserved)
{
    if (reason == DLL_PROCESS_ATTACH)
    {
        DisableThreadLibraryCalls(hModule);
        CreateThread(nullptr, 0, MainThread, hModule, 0, nullptr);
    }
    return TRUE;
}
