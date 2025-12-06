#include "pch.h"
#include "utils/console.h"
#include <cstdarg>
#include <chrono>
#include <iomanip>
#include <sstream>
#include <iostream>

namespace utils
{
    DebugConsole& DebugConsole::Get()
    {
        static DebugConsole instance;
        return instance;
    }

    bool DebugConsole::Initialize(const char* title)
    {
        if (m_active)
            return true;

        // Allocate a new console for this process
        if (!AllocConsole())
        {
            // Console might already exist
            if (GetLastError() != ERROR_ACCESS_DENIED)
                return false;
        }

        // Get console handle
        m_handle = GetStdHandle(STD_OUTPUT_HANDLE);
        if (m_handle == INVALID_HANDLE_VALUE)
            return false;

        // Redirect stdout/stderr/stdin
        freopen_s(&m_stdout, "CONOUT$", "w", stdout);
        freopen_s(&m_stderr, "CONOUT$", "w", stderr);
        freopen_s(&m_stdin, "CONIN$", "r", stdin);

        // Enable ANSI escape codes and virtual terminal processing
        DWORD mode = 0;
        GetConsoleMode(m_handle, &mode);
        SetConsoleMode(m_handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);

        // Set console title
        SetConsoleTitleA(title);

        // Get console window handle
        m_hwnd = GetConsoleWindow();

        // Set console buffer size
        COORD bufferSize = { 120, 9999 };
        SetConsoleScreenBufferSize(m_handle, bufferSize);

        // Set console window size
        SMALL_RECT windowSize = { 0, 0, 119, 40 };
        SetConsoleWindowInfo(m_handle, TRUE, &windowSize);

        // Set default color
        SetColor(m_defaultColor);

        m_active = true;

        // Print startup banner
        PrintSeparator('=', 60);
        PrintHeader("LETHAL MENU DEBUG CONSOLE");
        PrintSeparator('=', 60);

        auto now = std::chrono::system_clock::now();
        auto time = std::chrono::system_clock::to_time_t(now);
        struct tm timeInfo;
        localtime_s(&timeInfo, &time);

        std::ostringstream ss;
        ss << "Session started: " << std::put_time(&timeInfo, "%Y-%m-%d %H:%M:%S");
        WriteLine(ss.str(), ConsoleColor::DarkCyan);
        WriteLine("");

        return true;
    }

    void DebugConsole::Shutdown()
    {
        if (!m_active)
            return;

        PrintSeparator('=', 60);
        WriteLine("Console shutting down...", ConsoleColor::DarkYellow);

        if (m_stdout) fclose(m_stdout);
        if (m_stderr) fclose(m_stderr);
        if (m_stdin) fclose(m_stdin);

        FreeConsole();

        m_active = false;
        m_handle = INVALID_HANDLE_VALUE;
        m_hwnd = nullptr;
    }

    void DebugConsole::Write(const std::string& text)
    {
        Write(text.c_str());
    }

    void DebugConsole::Write(const char* text)
    {
        if (!m_active || !text)
            return;

        std::lock_guard<std::mutex> lock(m_mutex);
        WriteInternal(text, strlen(text));
    }

    void DebugConsole::Write(const std::string& text, ConsoleColor color)
    {
        Write(text.c_str(), color);
    }

    void DebugConsole::Write(const char* text, ConsoleColor color)
    {
        if (!m_active || !text)
            return;

        std::lock_guard<std::mutex> lock(m_mutex);
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(color));
        WriteInternal(text, strlen(text));
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(m_defaultColor));
    }

    void DebugConsole::WriteLine(const std::string& text)
    {
        WriteLine(text.c_str());
    }

    void DebugConsole::WriteLine(const char* text)
    {
        if (!m_active)
            return;

        std::lock_guard<std::mutex> lock(m_mutex);
        if (text)
            WriteInternal(text, strlen(text));
        WriteInternal("\n", 1);
    }

    void DebugConsole::WriteLine(const std::string& text, ConsoleColor color)
    {
        WriteLine(text.c_str(), color);
    }

    void DebugConsole::WriteLine(const char* text, ConsoleColor color)
    {
        if (!m_active)
            return;

        std::lock_guard<std::mutex> lock(m_mutex);
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(color));
        if (text)
            WriteInternal(text, strlen(text));
        WriteInternal("\n", 1);
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(m_defaultColor));
    }

    void DebugConsole::WriteFormat(const char* format, ...)
    {
        if (!m_active || !format)
            return;

        char buffer[4096];
        va_list args;
        va_start(args, format);
        vsnprintf(buffer, sizeof(buffer), format, args);
        va_end(args);

        Write(buffer);
    }

    void DebugConsole::WriteFormat(ConsoleColor color, const char* format, ...)
    {
        if (!m_active || !format)
            return;

        char buffer[4096];
        va_list args;
        va_start(args, format);
        vsnprintf(buffer, sizeof(buffer), format, args);
        va_end(args);

        Write(buffer, color);
    }

    void DebugConsole::LogInfo(const char* format, ...)
    {
        if (!m_active || !format)
            return;

        char buffer[4096];
        va_list args;
        va_start(args, format);
        vsnprintf(buffer, sizeof(buffer), format, args);
        va_end(args);

        std::lock_guard<std::mutex> lock(m_mutex);

        // Timestamp
        std::string ts = GetTimestamp();
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::DarkGray));
        WriteInternal(ts.c_str(), ts.length());

        // Tag
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::Cyan));
        WriteInternal(" [INFO] ", 8);

        // Message
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::White));
        WriteInternal(buffer, strlen(buffer));
        WriteInternal("\n", 1);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(m_defaultColor));
    }

    void DebugConsole::LogWarning(const char* format, ...)
    {
        if (!m_active || !format)
            return;

        char buffer[4096];
        va_list args;
        va_start(args, format);
        vsnprintf(buffer, sizeof(buffer), format, args);
        va_end(args);

        std::lock_guard<std::mutex> lock(m_mutex);

        std::string ts = GetTimestamp();
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::DarkGray));
        WriteInternal(ts.c_str(), ts.length());

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::Yellow));
        WriteInternal(" [WARN] ", 8);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::DarkYellow));
        WriteInternal(buffer, strlen(buffer));
        WriteInternal("\n", 1);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(m_defaultColor));
    }

    void DebugConsole::LogError(const char* format, ...)
    {
        if (!m_active || !format)
            return;

        char buffer[4096];
        va_list args;
        va_start(args, format);
        vsnprintf(buffer, sizeof(buffer), format, args);
        va_end(args);

        std::lock_guard<std::mutex> lock(m_mutex);

        std::string ts = GetTimestamp();
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::DarkGray));
        WriteInternal(ts.c_str(), ts.length());

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::Red));
        WriteInternal(" [ERROR] ", 9);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::DarkRed));
        WriteInternal(buffer, strlen(buffer));
        WriteInternal("\n", 1);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(m_defaultColor));
    }

    void DebugConsole::LogDebug(const char* format, ...)
    {
        if (!m_active || !format)
            return;

        char buffer[4096];
        va_list args;
        va_start(args, format);
        vsnprintf(buffer, sizeof(buffer), format, args);
        va_end(args);

        std::lock_guard<std::mutex> lock(m_mutex);

        std::string ts = GetTimestamp();
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::DarkGray));
        WriteInternal(ts.c_str(), ts.length());

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::Magenta));
        WriteInternal(" [DEBUG] ", 9);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::DarkMagenta));
        WriteInternal(buffer, strlen(buffer));
        WriteInternal("\n", 1);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(m_defaultColor));
    }

    void DebugConsole::LogSuccess(const char* format, ...)
    {
        if (!m_active || !format)
            return;

        char buffer[4096];
        va_list args;
        va_start(args, format);
        vsnprintf(buffer, sizeof(buffer), format, args);
        va_end(args);

        std::lock_guard<std::mutex> lock(m_mutex);

        std::string ts = GetTimestamp();
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::DarkGray));
        WriteInternal(ts.c_str(), ts.length());

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::Green));
        WriteInternal(" [OK] ", 6);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::DarkGreen));
        WriteInternal(buffer, strlen(buffer));
        WriteInternal("\n", 1);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(m_defaultColor));
    }

    void DebugConsole::PrintHeader(const char* title)
    {
        if (!m_active || !title)
            return;

        std::lock_guard<std::mutex> lock(m_mutex);

        size_t titleLen = strlen(title);
        size_t totalWidth = 60;
        size_t padding = (totalWidth - titleLen - 2) / 2;

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::Cyan));

        // Centered title
        for (size_t i = 0; i < padding; i++)
            WriteInternal(" ", 1);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::White));
        WriteInternal(title, titleLen);
        WriteInternal("\n", 1);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(m_defaultColor));
    }

    void DebugConsole::PrintSeparator(char c, int length)
    {
        if (!m_active)
            return;

        std::lock_guard<std::mutex> lock(m_mutex);

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(ConsoleColor::DarkGray));
        for (int i = 0; i < length; i++)
            WriteInternal(&c, 1);
        WriteInternal("\n", 1);
        SetConsoleTextAttribute(m_handle, static_cast<WORD>(m_defaultColor));
    }

    void DebugConsole::SetColor(ConsoleColor color)
    {
        if (!m_active)
            return;

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(color));
    }

    void DebugConsole::ResetColor()
    {
        if (!m_active)
            return;

        SetConsoleTextAttribute(m_handle, static_cast<WORD>(m_defaultColor));
    }

    void DebugConsole::Clear()
    {
        if (!m_active)
            return;

        std::lock_guard<std::mutex> lock(m_mutex);

        CONSOLE_SCREEN_BUFFER_INFO csbi;
        GetConsoleScreenBufferInfo(m_handle, &csbi);

        DWORD count;
        DWORD cellCount = csbi.dwSize.X * csbi.dwSize.Y;
        COORD homeCoords = { 0, 0 };

        FillConsoleOutputCharacterA(m_handle, ' ', cellCount, homeCoords, &count);
        FillConsoleOutputAttribute(m_handle, csbi.wAttributes, cellCount, homeCoords, &count);
        SetConsoleCursorPosition(m_handle, homeCoords);
    }

    std::string DebugConsole::GetTimestamp()
    {
        auto now = std::chrono::system_clock::now();
        auto time = std::chrono::system_clock::to_time_t(now);
        auto ms = std::chrono::duration_cast<std::chrono::milliseconds>(
            now.time_since_epoch()) % 1000;

        struct tm timeInfo;
        localtime_s(&timeInfo, &time);

        std::ostringstream ss;
        ss << std::put_time(&timeInfo, "[%H:%M:%S")
           << '.' << std::setfill('0') << std::setw(3) << ms.count() << "]";

        return ss.str();
    }

    void DebugConsole::WriteInternal(const char* text, size_t length)
    {
        DWORD written;
        WriteConsoleA(m_handle, text, static_cast<DWORD>(length), &written, nullptr);
    }
}
