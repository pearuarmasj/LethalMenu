#pragma once

#include <string>
#include <fstream>
#include <mutex>
#include <cstdio>
#include <cstdarg>

namespace utils
{
    enum class LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Success  // Added for operation confirmations
    };

    class Logger
    {
    public:
        static Logger& Get();

        void Initialize(const std::string& filename = "lethal_menu.log");
        void Shutdown();

        void Log(LogLevel level, const std::string& message);
        void LogFormat(LogLevel level, const char* fmt, ...);
        void Debug(const std::string& message) { Log(LogLevel::Debug, message); }
        void Info(const std::string& message) { Log(LogLevel::Info, message); }
        void Warning(const std::string& message) { Log(LogLevel::Warning, message); }
        void Error(const std::string& message) { Log(LogLevel::Error, message); }
        void Success(const std::string& message) { Log(LogLevel::Success, message); }

        void SetMinLevel(LogLevel level) { m_minLevel = level; }
        
        // Console output is now handled by DebugConsole
        void SetUseDebugConsole(bool use) { m_useDebugConsole = use; }

    private:
        Logger() = default;
        ~Logger();

        std::ofstream m_file;
        std::mutex m_mutex;
        LogLevel m_minLevel = LogLevel::Debug;  // Show everything by default in debug builds
        bool m_useDebugConsole = true;
        bool m_initialized = false;

        const char* LevelToString(LogLevel level);
    };
}

// Convenience macros with format support
#define LOG_DEBUG(...) utils::Logger::Get().LogFormat(utils::LogLevel::Debug, __VA_ARGS__)
#define LOG_INFO(...) utils::Logger::Get().LogFormat(utils::LogLevel::Info, __VA_ARGS__)
#define LOG_WARNING(...) utils::Logger::Get().LogFormat(utils::LogLevel::Warning, __VA_ARGS__)
#define LOG_WARN(...) utils::Logger::Get().LogFormat(utils::LogLevel::Warning, __VA_ARGS__)
#define LOG_ERROR(...) utils::Logger::Get().LogFormat(utils::LogLevel::Error, __VA_ARGS__)
#define LOG_SUCCESS(...) utils::Logger::Get().LogFormat(utils::LogLevel::Success, __VA_ARGS__)

