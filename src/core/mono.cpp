#include "pch.h"
#include "mono.h"
#include "utils/logger.h"
#include <unordered_map>
#include <string>

namespace core
{
    // For assembly enumeration callback
    struct AssemblySearchContext
    {
        const char* targetName;
        MonoAssembly* result;
        Mono* mono;
    };

    static void AssemblySearchCallback(MonoAssembly* assembly, void* userData)
    {
        auto* ctx = static_cast<AssemblySearchContext*>(userData);
        if (ctx->result)
            return; // Already found

        // Get assembly name
        auto* asmName = ctx->mono->fn_mono_assembly_get_name(assembly);
        if (!asmName)
            return;

        const char* name = ctx->mono->fn_mono_assembly_name_get_name(asmName);
        if (!name)
            return;

        if (strcmp(name, ctx->targetName) == 0)
        {
            ctx->result = assembly;
        }
    }

    // Debug: enumerate all assemblies
    struct AssemblyListContext
    {
        Mono* mono;
    };

    static void AssemblyListCallback(MonoAssembly* assembly, void* userData)
    {
        auto* ctx = static_cast<AssemblyListContext*>(userData);

        auto* asmName = ctx->mono->fn_mono_assembly_get_name(assembly);
        if (!asmName)
            return;

        const char* name = ctx->mono->fn_mono_assembly_name_get_name(asmName);
        if (name)
        {
            LOG_DEBUG("  Found assembly: %s", name);
        }
    }

    Mono& Mono::Get()
    {
        static Mono instance;
        return instance;
    }

    bool Mono::Initialize()
    {
        if (m_initialized)
            return true;

        // Unity uses mono-2.0-bdwgc.dll for newer versions
        m_monoModule = GetModuleHandleA("mono-2.0-bdwgc.dll");
        if (!m_monoModule)
        {
            // Fallback for older Unity versions
            m_monoModule = GetModuleHandleA("mono.dll");
        }

        if (!m_monoModule)
        {
            LOG_ERROR("Failed to find Mono module");
            return false;
        }

        if (!LoadMonoFunctions())
        {
            LOG_ERROR("Failed to load Mono functions");
            return false;
        }

        m_rootDomain = fn_mono_get_root_domain();
        if (!m_rootDomain)
        {
            LOG_ERROR("Failed to get Mono root domain");
            return false;
        }

        // Attach to the Mono thread
        fn_mono_thread_attach(m_rootDomain);

        m_initialized = true;
        LOG_INFO("Mono runtime initialized");

        return true;
    }

    void Mono::Shutdown()
    {
        if (!m_initialized)
            return;

        m_initialized = false;
    }

    bool Mono::LoadMonoFunctions()
    {
#define LOAD_MONO_FUNC(name) \
        fn_##name = reinterpret_cast<name##_t>(GetProcAddress(m_monoModule, #name)); \
        if (!fn_##name) { LOG_ERROR("Failed to find " #name); return false; }

        LOAD_MONO_FUNC(mono_get_root_domain);
        LOAD_MONO_FUNC(mono_domain_assembly_open);
        LOAD_MONO_FUNC(mono_assembly_get_image);
        LOAD_MONO_FUNC(mono_class_from_name);
        LOAD_MONO_FUNC(mono_class_get_method_from_name);
        LOAD_MONO_FUNC(mono_class_get_field_from_name);
        LOAD_MONO_FUNC(mono_class_get_property_from_name);
        LOAD_MONO_FUNC(mono_field_get_value);
        LOAD_MONO_FUNC(mono_field_set_value);
        LOAD_MONO_FUNC(mono_class_vtable);
        LOAD_MONO_FUNC(mono_field_static_get_value);
        LOAD_MONO_FUNC(mono_field_static_set_value);
        LOAD_MONO_FUNC(mono_property_get_get_method);
        LOAD_MONO_FUNC(mono_runtime_invoke);
        LOAD_MONO_FUNC(mono_string_to_utf8);
        LOAD_MONO_FUNC(mono_string_new);
        LOAD_MONO_FUNC(mono_array_length);
        LOAD_MONO_FUNC(mono_array_addr_with_size);
        LOAD_MONO_FUNC(mono_object_get_class);
        LOAD_MONO_FUNC(mono_object_unbox);
        LOAD_MONO_FUNC(mono_thread_attach);
        LOAD_MONO_FUNC(mono_thread_detach);
        LOAD_MONO_FUNC(mono_free);
        LOAD_MONO_FUNC(mono_domain_get);
        LOAD_MONO_FUNC(mono_assembly_foreach);
        LOAD_MONO_FUNC(mono_assembly_get_name);
        LOAD_MONO_FUNC(mono_assembly_name_get_name);
        LOAD_MONO_FUNC(mono_class_get_fields);
        LOAD_MONO_FUNC(mono_field_get_name);
        LOAD_MONO_FUNC(mono_field_get_offset);

#undef LOAD_MONO_FUNC

        // Optional function - may not exist in all Mono versions
        fn_mono_vtable_get_static_field_data = reinterpret_cast<mono_vtable_get_static_field_data_t>(
            GetProcAddress(m_monoModule, "mono_vtable_get_static_field_data"));
        if (fn_mono_vtable_get_static_field_data)
            LOG_INFO("mono_vtable_get_static_field_data found");
        else
            LOG_WARN("mono_vtable_get_static_field_data not found, using fallback");

        return true;
    }

    MonoAssembly* Mono::GetAssembly(const char* name)
    {
        if (!m_initialized)
            return nullptr;

        // Use assembly enumeration to find already-loaded assemblies
        AssemblySearchContext ctx{ name, nullptr, this };
        fn_mono_assembly_foreach(AssemblySearchCallback, &ctx);

        return ctx.result;
    }

    MonoImage* Mono::GetImage(MonoAssembly* assembly)
    {
        if (!assembly)
            return nullptr;

        return fn_mono_assembly_get_image(assembly);
    }

    MonoImage* Mono::GetImageByName(const char* assemblyName)
    {
        auto* assembly = GetAssembly(assemblyName);
        if (!assembly)
        {
            // Debug: list all loaded assemblies
            LOG_DEBUG("Assembly '%s' not found. Loaded assemblies:", assemblyName);
            AssemblyListContext listCtx{ this };
            fn_mono_assembly_foreach(AssemblyListCallback, &listCtx);
        }
        return GetImage(assembly);
    }

    MonoClass* Mono::GetClass(MonoImage* image, const char* nameSpace, const char* name)
    {
        if (!image)
            return nullptr;

        return fn_mono_class_from_name(image, nameSpace, name);
    }

    MonoClass* Mono::GetClassFromName(const char* assemblyName, const char* nameSpace, const char* className)
    {
        auto* image = GetImageByName(assemblyName);
        return GetClass(image, nameSpace, className);
    }

    MonoMethod* Mono::GetMethod(MonoClass* klass, const char* name, int paramCount)
    {
        if (!klass)
            return nullptr;

        return fn_mono_class_get_method_from_name(klass, name, paramCount);
    }

    MonoClassField* Mono::GetField(MonoClass* klass, const char* name)
    {
        if (!klass)
            return nullptr;

        return fn_mono_class_get_field_from_name(klass, name);
    }

    void Mono::GetFieldValueInternal(MonoObject* obj, MonoClassField* field, void* value)
    {
        if (!obj || !field)
            return;

        __try
        {
            fn_mono_field_get_value(obj, field, value);
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            // Leave value as-is on failure
        }
    }

    void Mono::SetFieldValueInternal(MonoObject* obj, MonoClassField* field, void* value)
    {
        if (!obj || !field)
            return;

        __try
        {
            fn_mono_field_set_value(obj, field, value);
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            // Silently fail on exception
        }
    }

    void* Mono::GetFieldValue(MonoObject* obj, MonoClassField* field)
    {
        void* value = nullptr;
        GetFieldValueInternal(obj, field, &value);
        return value;
    }

    void Mono::SetFieldValue(MonoObject* obj, MonoClassField* field, void* value)
    {
        SetFieldValueInternal(obj, field, &value);
    }

    void Mono::GetStaticFieldValueInternal(MonoClass* klass, MonoClassField* field, void* value)
    {
        if (!klass || !field || !m_rootDomain)
            return;

        // SEH protection - Mono can throw internal exceptions if field access fails
        __try
        {
            // Get the VTable for the class - required for static field access
            void* vtable = fn_mono_class_vtable(m_rootDomain, klass);
            if (!vtable)
                return;

            // Try mono_vtable_get_static_field_data if available (more reliable)
            if (fn_mono_vtable_get_static_field_data)
            {
                void* staticData = fn_mono_vtable_get_static_field_data(vtable);
                if (staticData)
                {
                    uint32_t offset = fn_mono_field_get_offset(field);
                    // Static field offsets are relative to the static data block
                    void** fieldPtr = reinterpret_cast<void**>(static_cast<char*>(staticData) + offset);
                    *static_cast<void**>(value) = *fieldPtr;
                    return;
                }
            }

            // Fallback to standard method
            fn_mono_field_static_get_value(vtable, field, value);
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            // Mono threw - leave value as-is (nullptr by default)
        }
    }

    void* Mono::GetStaticFieldValue(MonoClass* klass, MonoClassField* field)
    {
        void* value = nullptr;
        GetStaticFieldValueInternal(klass, field, &value);
        return value;
    }

    void Mono::SetStaticFieldValue(MonoClass* klass, MonoClassField* field, void* value)
    {
        if (!klass || !field || !m_rootDomain)
            return;

        __try
        {
            void* vtable = fn_mono_class_vtable(m_rootDomain, klass);
            if (!vtable)
                return;

            fn_mono_field_static_set_value(vtable, field, &value);
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            // Silently fail on exception
        }
    }

    MonoProperty* Mono::GetProperty(MonoClass* klass, const char* name)
    {
        if (!klass)
            return nullptr;

        return fn_mono_class_get_property_from_name(klass, name);
    }

    MonoObject* Mono::GetPropertyValue(MonoObject* obj, MonoProperty* prop)
    {
        if (!prop)
            return nullptr;

        auto* getter = fn_mono_property_get_get_method(prop);
        if (!getter)
            return nullptr;

        return InvokeMethod(getter, obj, nullptr);
    }

    MonoObject* Mono::InvokeMethod(MonoMethod* method, void* obj, void** params)
    {
        if (!method)
            return nullptr;

        MonoObject* result = nullptr;
        __try
        {
            MonoObject* exception = nullptr;
            result = fn_mono_runtime_invoke(method, obj, params, &exception);

            if (exception)
                return nullptr;
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            return nullptr;
        }

        return result;
    }

    std::string Mono::MonoStringToUTF8(MonoString* str)
    {
        if (!str)
            return "";

        char* utf8 = fn_mono_string_to_utf8(str);
        if (!utf8)
            return "";

        std::string result(utf8);
        fn_mono_free(utf8);
        return result;
    }

    MonoString* Mono::UTF8ToMonoString(const char* str)
    {
        if (!str)
            return nullptr;

        return fn_mono_string_new(m_rootDomain, str);
    }

    uintptr_t Mono::GetArrayLength(MonoArray* arr)
    {
        if (!arr)
            return 0;

        return fn_mono_array_length(arr);
    }

    void* Mono::GetArrayElement(MonoArray* arr, uintptr_t index)
    {
        if (!arr)
            return nullptr;

        // For reference types (objects), element size is pointer size
        char* addr = fn_mono_array_addr_with_size(arr, sizeof(void*), index);
        return *reinterpret_cast<void**>(addr);
    }

    MonoClass* Mono::GetObjectClass(MonoObject* obj)
    {
        if (!obj)
            return nullptr;

        return fn_mono_object_get_class(obj);
    }

    void* Mono::Unbox(MonoObject* obj)
    {
        if (!obj)
            return nullptr;

        return fn_mono_object_unbox(obj);
    }

    void Mono::EnumerateClassFields(MonoClass* klass)
    {
        if (!klass)
        {
            LOG_INFO("EnumerateClassFields: null class");
            return;
        }

        LOG_INFO("Enumerating fields for class %p:", klass);

        void* iter = nullptr;
        MonoClassField* field;
        int count = 0;

        while ((field = fn_mono_class_get_fields(klass, &iter)) != nullptr)
        {
            const char* name = fn_mono_field_get_name(field);
            LOG_INFO("  Field %d: '%s' (%p)", count, name ? name : "<null>", field);
            count++;
        }

        LOG_INFO("Total fields enumerated: %d", count);
    }

    MonoThread* Mono::AttachThread()
    {
        if (!m_initialized)
            return nullptr;

        return fn_mono_thread_attach(m_rootDomain);
    }

    void Mono::DetachThread(MonoThread* thread)
    {
        if (!thread)
            return;

        fn_mono_thread_detach(thread);
    }
}
