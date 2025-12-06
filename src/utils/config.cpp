#include "pch.h"
#include "config.h"
#include <fstream>
#include <ShlObj.h>

namespace utils
{
    Config& Config::Get()
    {
        static Config instance;
        return instance;
    }

    std::string Config::GetConfigDirectory()
    {
        if (!m_configPath.empty())
            return m_configPath;

        char path[MAX_PATH];
        if (SUCCEEDED(SHGetFolderPathA(nullptr, CSIDL_APPDATA, nullptr, 0, path)))
        {
            m_configPath = std::string(path) + "\\LethalMenu\\";
            CreateDirectoryA(m_configPath.c_str(), nullptr);
        }
        else
        {
            m_configPath = ".\\";
        }

        return m_configPath;
    }

    bool Config::Load(const std::string& filename)
    {
        std::string fullPath = GetConfigDirectory() + filename;

        std::ifstream file(fullPath);
        if (!file.is_open())
            return false;

        try
        {
            file >> m_data;
            return true;
        }
        catch (const nlohmann::json::exception&)
        {
            return false;
        }
    }

    bool Config::Save(const std::string& filename)
    {
        std::string fullPath = GetConfigDirectory() + filename;

        std::ofstream file(fullPath);
        if (!file.is_open())
            return false;

        try
        {
            file << m_data.dump(4);
            return true;
        }
        catch (const nlohmann::json::exception&)
        {
            return false;
        }
    }
}
