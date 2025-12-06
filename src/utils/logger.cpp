#include "pch.h"
#include "logger.h"
#include "console.h"
#include <chrono>
#include <iomanip>
#include <sstream>
#include <iostream>

namespace utils
{
    Logger& Logger::Get()
    {
        static Logger instance;
        return instance;
    }

    Logger::~Logger()
    {
        Shutdown();
    }

    void Logger::Initialize(const std::string& filename)
    {
        if (m_initialized)
            return;

        m_file.open(filename, std::ios::out | std::ios::trunc);
        m_initialized = m_file.is_open();

        if (m_initialized)
        {
            Info("Logger initialized - file output active");
        }
    }

    void Logger::Shutdown()
    {
        if (!m_initialized)
            return;

        Info("Logger shutting down");
        m_file.close();
        m_initialized = false;
    }

    void Logger::LogFormat(LogLevel level, const char* fmt, ...)
    {
        if (level < m_minLevel)
            return;

        char buffer[4096];
        va_list args;
        va_start(args, fmt);
        vsnprintf(buffer, sizeof(buffer), fmt, args);
        va_end(args);

        Log(level, std::string(buffer));
    }

    void Logger::Log(LogLevel level, const std::string& message)
    {
        if (level < m_minLevel)
            return;

        std::lock_guard<std::mutex> lock(m_mutex);

        // Get current time
        auto now = std::chrono::system_clock::now();
        auto time = std::chrono::system_clock::to_time_t(now);
        std::tm tm;
        localtime_s(&tm, &time);

        std::ostringstream ss;
        ss << std::put_time(&tm, "[%H:%M:%S] ");
        ss << "[" << LevelToString(level) << "] ";
        ss << message;

        std::string logLine = ss.str();

        // Write to file
        if (m_file.is_open())
        {
            m_file << logLine << std::endl;
            m_file.flush();
        }

        // Write to debug console with color coding
        if (m_useDebugConsole && DebugConsole::Get().IsActive())
        {
            switch (level)
            {
            case LogLevel::Debug:
                DebugConsole::Get().LogDebug("%s", message.c_str());
                break;
            case LogLevel::Info:
                DebugConsole::Get().LogInfo("%s", message.c_str());
                break;
            case LogLevel::Warning:
                DebugConsole::Get().LogWarning("%s", message.c_str());
                break;
            case LogLevel::Error:
                DebugConsole::Get().LogError("%s", message.c_str());
                break;
            case LogLevel::Success:
                DebugConsole::Get().LogSuccess("%s", message.c_str());
                break;
            }
        }
    }

    const char* Logger::LevelToString(LogLevel level)
    {
        switch (level)
        {
        case LogLevel::Debug:   return "DEBUG";
        case LogLevel::Info:    return "INFO";
        case LogLevel::Warning: return "WARN";
        case LogLevel::Error:   return "ERROR";
        case LogLevel::Success: return "OK";
        default:                return "UNKNOWN";
        }
    }
}
