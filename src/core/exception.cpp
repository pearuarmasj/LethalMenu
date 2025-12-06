#include "pch.h"
#include "core/exception.h"
#include "utils/logger.h"
#include <sstream>
#include <iomanip>

namespace core
{
    ExceptionHandler& ExceptionHandler::Get()
    {
        static ExceptionHandler instance;
        return instance;
    }

    bool ExceptionHandler::Initialize()
    {
        if (m_initialized)
            return true;

        InitializeCriticalSection(&m_cs);

        // Initialize symbol handler for stack traces
        SymSetOptions(SYMOPT_UNDNAME | SYMOPT_DEFERRED_LOADS | SYMOPT_LOAD_LINES);

        HANDLE process = GetCurrentProcess();
        if (SymInitialize(process, nullptr, TRUE))
        {
            m_symbolsLoaded = true;
            LOG_INFO("[SEH] Symbol handler initialized");
        }
        else
        {
            LOG_WARNING("[SEH] Failed to initialize symbols (error: %lu). Stack traces will be limited.", GetLastError());
        }

        m_initialized = true;
        LOG_INFO("[SEH] Exception handler framework initialized");
        return true;
    }

    void ExceptionHandler::Shutdown()
    {
        if (!m_initialized)
            return;

        if (m_previousFilter)
        {
            SetUnhandledExceptionFilter(m_previousFilter);
            m_previousFilter = nullptr;
        }

        if (m_symbolsLoaded)
        {
            SymCleanup(GetCurrentProcess());
            m_symbolsLoaded = false;
        }

        DeleteCriticalSection(&m_cs);
        m_initialized = false;

        LOG_INFO("[SEH] Exception handler shutdown");
    }

    void ExceptionHandler::InstallGlobalHandler()
    {
        m_previousFilter = SetUnhandledExceptionFilter(GlobalExceptionFilter);
        LOG_INFO("[SEH] Global exception filter installed");
    }

    LONG WINAPI ExceptionHandler::GlobalExceptionFilter(EXCEPTION_POINTERS* pExceptionInfo)
    {
        Get().HandleException(pExceptionInfo, "UNHANDLED", std::source_location::current());

        // Continue searching for other handlers
        return EXCEPTION_CONTINUE_SEARCH;
    }

    void ExceptionHandler::HandleException(
        EXCEPTION_POINTERS* pExceptionInfo,
        const char* operationName,
        const std::source_location& loc)
    {
        EnterCriticalSection(&m_cs);

        auto* record = pExceptionInfo->ExceptionRecord;
        auto* ctx = pExceptionInfo->ContextRecord;

        m_lastException.code = record->ExceptionCode;
        m_lastException.address = record->ExceptionAddress;
        m_lastException.description = GetExceptionCodeString(record->ExceptionCode);
        m_lastException.moduleName = GetModuleFromAddress(record->ExceptionAddress);
        m_lastException.functionName = GetSymbolFromAddress(record->ExceptionAddress);
        m_lastException.fileName = loc.file_name();
        m_lastException.lineNumber = static_cast<int>(loc.line());
        m_lastException.stackTrace = GenerateStackTrace(ctx);
        m_lastException.registerDump = GenerateRegisterDump(ctx);

        // Log everything
        LOG_ERROR("========== EXCEPTION CAUGHT ==========");
        LOG_ERROR("Operation: %s", operationName);
        LOG_ERROR("Code: 0x%08X (%s)", m_lastException.code, m_lastException.description.c_str());
        LOG_ERROR("Address: 0x%p", m_lastException.address);
        LOG_ERROR("Module: %s", m_lastException.moduleName.c_str());
        LOG_ERROR("Function: %s", m_lastException.functionName.c_str());
        LOG_ERROR("Source: %s:%d", m_lastException.fileName.c_str(), m_lastException.lineNumber);
        LOG_ERROR("--- Stack Trace ---");
        LOG_ERROR("%s", m_lastException.stackTrace.c_str());
        LOG_ERROR("--- Registers ---");
        LOG_ERROR("%s", m_lastException.registerDump.c_str());
        LOG_ERROR("=====================================");

        LeaveCriticalSection(&m_cs);
    }

    // Helper function that can use SEH (no C++ objects with destructors)
    static bool ExecuteSEHRaw(
        void (*invoker)(void*),
        void* funcPtr,
        DWORD* outExceptionCode)
    {
        *outExceptionCode = 0;

        __try
        {
            invoker(funcPtr);
            return true;
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            *outExceptionCode = GetExceptionCode();
            return false;
        }
    }

    SafeResult<void> ExceptionHandler::ExecuteSEH(
        const char* operationName,
        void (*invoker)(void*),
        void* funcPtr,
        const std::source_location& loc)
    {
        SafeResult<void> result{ true, "" };
        DWORD exceptionCode = 0;

        bool success = ExecuteSEHRaw(invoker, funcPtr, &exceptionCode);

        if (!success)
        {
            // Handle the exception info (this is safe to call after SEH block)
            result.success = false;
            char buf[256];
            snprintf(buf, sizeof(buf), "Exception 0x%08X (%s) in %s",
                exceptionCode,
                GetExceptionCodeString(exceptionCode).c_str(),
                operationName);
            result.error = buf;
            LOG_ERROR("%s", result.error.c_str());
        }

        return result;
    }

    std::string ExceptionHandler::GetExceptionCodeString(DWORD code)
    {
        switch (code)
        {
        case EXCEPTION_ACCESS_VIOLATION:         return "ACCESS_VIOLATION";
        case EXCEPTION_ARRAY_BOUNDS_EXCEEDED:    return "ARRAY_BOUNDS_EXCEEDED";
        case EXCEPTION_BREAKPOINT:               return "BREAKPOINT";
        case EXCEPTION_DATATYPE_MISALIGNMENT:    return "DATATYPE_MISALIGNMENT";
        case EXCEPTION_FLT_DENORMAL_OPERAND:     return "FLT_DENORMAL_OPERAND";
        case EXCEPTION_FLT_DIVIDE_BY_ZERO:       return "FLT_DIVIDE_BY_ZERO";
        case EXCEPTION_FLT_INEXACT_RESULT:       return "FLT_INEXACT_RESULT";
        case EXCEPTION_FLT_INVALID_OPERATION:    return "FLT_INVALID_OPERATION";
        case EXCEPTION_FLT_OVERFLOW:             return "FLT_OVERFLOW";
        case EXCEPTION_FLT_STACK_CHECK:          return "FLT_STACK_CHECK";
        case EXCEPTION_FLT_UNDERFLOW:            return "FLT_UNDERFLOW";
        case EXCEPTION_ILLEGAL_INSTRUCTION:      return "ILLEGAL_INSTRUCTION";
        case EXCEPTION_IN_PAGE_ERROR:            return "IN_PAGE_ERROR";
        case EXCEPTION_INT_DIVIDE_BY_ZERO:       return "INT_DIVIDE_BY_ZERO";
        case EXCEPTION_INT_OVERFLOW:             return "INT_OVERFLOW";
        case EXCEPTION_INVALID_DISPOSITION:      return "INVALID_DISPOSITION";
        case EXCEPTION_NONCONTINUABLE_EXCEPTION: return "NONCONTINUABLE_EXCEPTION";
        case EXCEPTION_PRIV_INSTRUCTION:         return "PRIV_INSTRUCTION";
        case EXCEPTION_SINGLE_STEP:              return "SINGLE_STEP";
        case EXCEPTION_STACK_OVERFLOW:           return "STACK_OVERFLOW";
        case 0xE06D7363:                         return "CPP_EXCEPTION"; // MSVC C++ exception
        default:
        {
            std::ostringstream ss;
            ss << "UNKNOWN_0x" << std::hex << std::uppercase << code;
            return ss.str();
        }
        }
    }

    std::string ExceptionHandler::GenerateStackTrace(CONTEXT* ctx, int maxFrames)
    {
        std::ostringstream ss;

        if (!m_symbolsLoaded)
        {
            ss << "  [Symbols not loaded - stack trace unavailable]\n";
            return ss.str();
        }

        HANDLE process = GetCurrentProcess();
        HANDLE thread = GetCurrentThread();

        STACKFRAME64 frame{};
        frame.AddrPC.Mode = AddrModeFlat;
        frame.AddrFrame.Mode = AddrModeFlat;
        frame.AddrStack.Mode = AddrModeFlat;

#ifdef _M_X64
        DWORD machineType = IMAGE_FILE_MACHINE_AMD64;
        frame.AddrPC.Offset = ctx->Rip;
        frame.AddrFrame.Offset = ctx->Rbp;
        frame.AddrStack.Offset = ctx->Rsp;
#else
        DWORD machineType = IMAGE_FILE_MACHINE_I386;
        frame.AddrPC.Offset = ctx->Eip;
        frame.AddrFrame.Offset = ctx->Ebp;
        frame.AddrStack.Offset = ctx->Esp;
#endif

        // Symbol buffer
        char symbolBuffer[sizeof(SYMBOL_INFO) + MAX_SYM_NAME * sizeof(TCHAR)];
        PSYMBOL_INFO symbol = reinterpret_cast<PSYMBOL_INFO>(symbolBuffer);
        symbol->SizeOfStruct = sizeof(SYMBOL_INFO);
        symbol->MaxNameLen = MAX_SYM_NAME;

        IMAGEHLP_LINE64 line{};
        line.SizeOfStruct = sizeof(IMAGEHLP_LINE64);

        int frameNum = 0;
        while (frameNum < maxFrames &&
               StackWalk64(machineType, process, thread, &frame, ctx,
                          nullptr, SymFunctionTableAccess64, SymGetModuleBase64, nullptr))
        {
            if (frame.AddrPC.Offset == 0)
                break;

            ss << "  [" << std::setw(2) << frameNum << "] ";

            DWORD64 displacement64 = 0;
            if (SymFromAddr(process, frame.AddrPC.Offset, &displacement64, symbol))
            {
                ss << symbol->Name;

                DWORD displacement32 = 0;
                if (SymGetLineFromAddr64(process, frame.AddrPC.Offset, &displacement32, &line))
                {
                    ss << " (" << line.FileName << ":" << line.LineNumber << ")";
                }

                ss << " +0x" << std::hex << displacement64;
            }
            else
            {
                ss << "0x" << std::hex << frame.AddrPC.Offset;
            }

            ss << " [" << GetModuleFromAddress(reinterpret_cast<void*>(frame.AddrPC.Offset)) << "]";
            ss << "\n";

            frameNum++;
        }

        if (frameNum == 0)
        {
            ss << "  [No stack frames captured]\n";
        }

        return ss.str();
    }

    std::string ExceptionHandler::GenerateRegisterDump(CONTEXT* ctx)
    {
        std::ostringstream ss;
        ss << std::hex << std::uppercase;

#ifdef _M_X64
        ss << "  RAX=" << std::setw(16) << std::setfill('0') << ctx->Rax
           << "  RBX=" << std::setw(16) << std::setfill('0') << ctx->Rbx
           << "  RCX=" << std::setw(16) << std::setfill('0') << ctx->Rcx << "\n";
        ss << "  RDX=" << std::setw(16) << std::setfill('0') << ctx->Rdx
           << "  RSI=" << std::setw(16) << std::setfill('0') << ctx->Rsi
           << "  RDI=" << std::setw(16) << std::setfill('0') << ctx->Rdi << "\n";
        ss << "  RBP=" << std::setw(16) << std::setfill('0') << ctx->Rbp
           << "  RSP=" << std::setw(16) << std::setfill('0') << ctx->Rsp
           << "  RIP=" << std::setw(16) << std::setfill('0') << ctx->Rip << "\n";
        ss << "  R8 =" << std::setw(16) << std::setfill('0') << ctx->R8
           << "  R9 =" << std::setw(16) << std::setfill('0') << ctx->R9
           << "  R10=" << std::setw(16) << std::setfill('0') << ctx->R10 << "\n";
        ss << "  R11=" << std::setw(16) << std::setfill('0') << ctx->R11
           << "  R12=" << std::setw(16) << std::setfill('0') << ctx->R12
           << "  R13=" << std::setw(16) << std::setfill('0') << ctx->R13 << "\n";
        ss << "  R14=" << std::setw(16) << std::setfill('0') << ctx->R14
           << "  R15=" << std::setw(16) << std::setfill('0') << ctx->R15 << "\n";
        ss << "  FLAGS=" << std::setw(8) << std::setfill('0') << ctx->EFlags << "\n";
#else
        ss << "  EAX=" << std::setw(8) << std::setfill('0') << ctx->Eax
           << "  EBX=" << std::setw(8) << std::setfill('0') << ctx->Ebx
           << "  ECX=" << std::setw(8) << std::setfill('0') << ctx->Ecx
           << "  EDX=" << std::setw(8) << std::setfill('0') << ctx->Edx << "\n";
        ss << "  ESI=" << std::setw(8) << std::setfill('0') << ctx->Esi
           << "  EDI=" << std::setw(8) << std::setfill('0') << ctx->Edi
           << "  EBP=" << std::setw(8) << std::setfill('0') << ctx->Ebp
           << "  ESP=" << std::setw(8) << std::setfill('0') << ctx->Esp << "\n";
        ss << "  EIP=" << std::setw(8) << std::setfill('0') << ctx->Eip
           << "  FLAGS=" << std::setw(8) << std::setfill('0') << ctx->EFlags << "\n";
#endif

        return ss.str();
    }

    std::string ExceptionHandler::GetModuleFromAddress(void* address)
    {
        HMODULE hModule = nullptr;
        if (GetModuleHandleExA(
            GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,
            static_cast<LPCSTR>(address),
            &hModule))
        {
            char moduleName[MAX_PATH];
            if (GetModuleFileNameA(hModule, moduleName, MAX_PATH))
            {
                // Extract just the filename
                const char* lastSlash = strrchr(moduleName, '\\');
                return lastSlash ? lastSlash + 1 : moduleName;
            }
        }
        return "unknown";
    }

    std::string ExceptionHandler::GetSymbolFromAddress(void* address)
    {
        if (!m_symbolsLoaded)
            return "symbols_unavailable";

        char buffer[sizeof(SYMBOL_INFO) + MAX_SYM_NAME * sizeof(TCHAR)];
        PSYMBOL_INFO symbol = reinterpret_cast<PSYMBOL_INFO>(buffer);
        symbol->SizeOfStruct = sizeof(SYMBOL_INFO);
        symbol->MaxNameLen = MAX_SYM_NAME;

        DWORD64 displacement = 0;
        if (SymFromAddr(GetCurrentProcess(), reinterpret_cast<DWORD64>(address), &displacement, symbol))
        {
            return symbol->Name;
        }
        return "unknown_function";
    }

    bool IsMemoryReadable(void* address, size_t size)
    {
        MEMORY_BASIC_INFORMATION mbi{};
        if (VirtualQuery(address, &mbi, sizeof(mbi)) == 0)
            return false;

        if (mbi.State != MEM_COMMIT)
            return false;

        DWORD protect = mbi.Protect;
        return (protect & PAGE_READONLY) ||
               (protect & PAGE_READWRITE) ||
               (protect & PAGE_EXECUTE_READ) ||
               (protect & PAGE_EXECUTE_READWRITE);
    }

    bool IsMemoryWritable(void* address, size_t size)
    {
        MEMORY_BASIC_INFORMATION mbi{};
        if (VirtualQuery(address, &mbi, sizeof(mbi)) == 0)
            return false;

        if (mbi.State != MEM_COMMIT)
            return false;

        DWORD protect = mbi.Protect;
        return (protect & PAGE_READWRITE) ||
               (protect & PAGE_EXECUTE_READWRITE);
    }
}
