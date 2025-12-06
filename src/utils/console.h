#pragma once

#include <Windows.h>
#include <string>
#include <mutex>

namespace utils
{
    enum class ConsoleColor : WORD
    {
        Black       = 0,
        DarkBlue    = FOREGROUND_BLUE,
        DarkGreen   = FOREGROUND_GREEN,
        DarkCyan    = FOREGROUND_GREEN | FOREGROUND_BLUE,
        DarkRed     = FOREGROUND_RED,
        DarkMagenta = FOREGROUND_RED | FOREGROUND_BLUE,
        DarkYellow  = FOREGROUND_RED | FOREGROUND_GREEN,
        Gray        = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE,
        DarkGray    = FOREGROUND_INTENSITY,
        Blue        = FOREGROUND_BLUE | FOREGROUND_INTENSITY,
        Green       = FOREGROUND_GREEN | FOREGROUND_INTENSITY,
        Cyan        = FOREGROUND_GREEN | FOREGROUND_BLUE | FOREGROUND_INTENSITY,
        Red         = FOREGROUND_RED | FOREGROUND_INTENSITY,
        Magenta     = FOREGROUND_RED | FOREGROUND_BLUE | FOREGROUND_INTENSITY,
        Yellow      = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_INTENSITY,
        White       = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE | FOREGROUND_INTENSITY
    };

    class DebugConsole
    {
    public:
        static DebugConsole& Get();

        // Initialize console (allocates new console window)
        bool Initialize(const char* title = "LethalMenu Debug Console");

        // Shutdown and free console
        void Shutdown();

        // Check if console is active
        bool IsActive() const { return m_active; }

        // Write raw text
        void Write(const std::string& text);
        void Write(const char* text);

        // Write with color
        void Write(const std::string& text, ConsoleColor color);
        void Write(const char* text, ConsoleColor color);

        // Write line (with newline)
        void WriteLine(const std::string& text);
        void WriteLine(const char* text);
        void WriteLine(const std::string& text, ConsoleColor color);
        void WriteLine(const char* text, ConsoleColor color);

        // Write formatted (printf style)
        void WriteFormat(const char* format, ...);
        void WriteFormat(ConsoleColor color, const char* format, ...);

        // Specialized logging methods
        void LogInfo(const char* format, ...);
        void LogWarning(const char* format, ...);
        void LogError(const char* format, ...);
        void LogDebug(const char* format, ...);
        void LogSuccess(const char* format, ...);

        // Section headers
        void PrintHeader(const char* title);
        void PrintSeparator(char c = '-', int length = 60);

        // Set console color
        void SetColor(ConsoleColor color);
        void ResetColor();

        // Clear console
        void Clear();

        // Get console window handle
        HWND GetWindowHandle() const { return m_hwnd; }

    private:
        DebugConsole() = default;

        std::string GetTimestamp();
        void WriteInternal(const char* text, size_t length);

        bool m_active = false;
        HANDLE m_handle = INVALID_HANDLE_VALUE;
        HWND m_hwnd = nullptr;
        FILE* m_stdout = nullptr;
        FILE* m_stderr = nullptr;
        FILE* m_stdin = nullptr;
        ConsoleColor m_defaultColor = ConsoleColor::Gray;
        std::mutex m_mutex;
    };
}
