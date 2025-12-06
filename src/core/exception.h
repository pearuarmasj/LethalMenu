#pragma once

#include <Windows.h>
#include <DbgHelp.h>
#include <string>
#include <source_location>

#pragma comment(lib, "DbgHelp.lib")

namespace core
{
    // Exception context information
    struct ExceptionContext
    {
        DWORD code;
        void* address;
        std::string moduleName;
        std::string functionName;
        std::string fileName;
        int lineNumber;
        std::string stackTrace;
        std::string registerDump;
        std::string description;
    };

    // Result wrapper for safe operations
    template<typename T>
    struct SafeResult
    {
        bool success;
        T value;
        std::string error;

        operator bool() const { return success; }
        T& operator*() { return value; }
        const T& operator*() const { return value; }
    };

    template<>
    struct SafeResult<void>
    {
        bool success;
        std::string error;

        operator bool() const { return success; }
    };

    class ExceptionHandler
    {
    public:
        static ExceptionHandler& Get();

        // Initialize the exception handler (call once at startup)
        bool Initialize();
        void Shutdown();

        // Install global exception filter
        void InstallGlobalHandler();

        // Execute a function with SEH protection - uses lambda wrapper approach
        template<typename Func>
        SafeResult<void> Execute(
            const char* operationName,
            Func&& func,
            const std::source_location& loc = std::source_location::current())
        {
            // Store lambda in thread-local to avoid passing through SEH boundary
            static thread_local void* s_funcPtr = nullptr;
            static thread_local void (*s_invoker)(void*) = nullptr;

            s_funcPtr = &func;
            s_invoker = [](void* p) { (*static_cast<Func*>(p))(); };

            return ExecuteSEH(operationName, s_invoker, s_funcPtr, loc);
        }

        // Get last exception context
        const ExceptionContext& GetLastException() const { return m_lastException; }

        // Convert exception code to readable string
        static std::string GetExceptionCodeString(DWORD code);

        // Generate stack trace from context
        std::string GenerateStackTrace(CONTEXT* ctx, int maxFrames = 32);

        // Generate register dump
        std::string GenerateRegisterDump(CONTEXT* ctx);

    private:
        ExceptionHandler() = default;

        // Internal SEH execution - no C++ objects with destructors allowed
        SafeResult<void> ExecuteSEH(
            const char* operationName,
            void (*invoker)(void*),
            void* funcPtr,
            const std::source_location& loc);

        static LONG WINAPI GlobalExceptionFilter(EXCEPTION_POINTERS* pExceptionInfo);
        void HandleException(EXCEPTION_POINTERS* pExceptionInfo, const char* operationName, const std::source_location& loc);

        std::string GetModuleFromAddress(void* address);
        std::string GetSymbolFromAddress(void* address);

        bool m_initialized = false;
        bool m_symbolsLoaded = false;
        LPTOP_LEVEL_EXCEPTION_FILTER m_previousFilter = nullptr;
        ExceptionContext m_lastException{};
        CRITICAL_SECTION m_cs{};
    };

    // Convenience macros for common patterns
    #define SEH_TRY(name, code) \
        core::ExceptionHandler::Get().Execute(name, [&]() { code; })

    // Safe pointer access
    template<typename T>
    T* SafePtr(T* ptr, const char* name = "pointer")
    {
        if (!ptr)
        {
            return nullptr;
        }

        __try
        {
            // Touch the memory to verify it's accessible
            volatile char test = *reinterpret_cast<volatile char*>(ptr);
            (void)test;
            return ptr;
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            return nullptr;
        }
    }

    // Safe memory read
    template<typename T>
    bool SafeRead(void* address, T& out)
    {
        __try
        {
            out = *reinterpret_cast<T*>(address);
            return true;
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            return false;
        }
    }

    // Safe memory write
    template<typename T>
    bool SafeWrite(void* address, const T& value)
    {
        __try
        {
            *reinterpret_cast<T*>(address) = value;
            return true;
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            return false;
        }
    }

    // Check if memory is readable
    bool IsMemoryReadable(void* address, size_t size);

    // Check if memory is writable
    bool IsMemoryWritable(void* address, size_t size);
}
