#pragma once

#include <string>
#include <nlohmann/json.hpp>

namespace utils
{
    class Config
    {
    public:
        static Config& Get();

        bool Load(const std::string& filename = "config.json");
        bool Save(const std::string& filename = "config.json");

        // Accessors for feature states
        nlohmann::json& Data() { return m_data; }

    private:
        Config() = default;

        nlohmann::json m_data;
        std::string m_configPath;

        std::string GetConfigDirectory();
    };
}
