#pragma once

#include <cstdint>
#include <string>

// Mono runtime types (opaque pointers)
typedef struct _MonoDomain MonoDomain;
typedef struct _MonoAssembly MonoAssembly;
typedef struct _MonoImage MonoImage;
typedef struct _MonoClass MonoClass;
typedef struct _MonoMethod MonoMethod;
typedef struct _MonoProperty MonoProperty;
typedef struct _MonoClassField MonoClassField;
typedef struct _MonoObject MonoObject;
typedef struct _MonoString MonoString;
typedef struct _MonoArray MonoArray;
typedef struct _MonoType MonoType;
typedef struct _MonoThread MonoThread;

namespace core
{
    class Mono
    {
    public:
        static Mono& Get();

        bool Initialize();
        void Shutdown();

        // Domain
        MonoDomain* GetRootDomain() const { return m_rootDomain; }

        // Assembly/Image lookup
        MonoAssembly* GetAssembly(const char* name);
        MonoImage* GetImage(MonoAssembly* assembly);
        MonoImage* GetImageByName(const char* assemblyName);

        // Class lookup
        MonoClass* GetClass(MonoImage* image, const char* nameSpace, const char* name);
        MonoClass* GetClassFromName(const char* assemblyName, const char* nameSpace, const char* className);

        // Method lookup
        MonoMethod* GetMethod(MonoClass* klass, const char* name, int paramCount = -1);
        MonoMethod* GetMethodFromDesc(MonoClass* klass, const char* desc, bool includeNamespace = false);

        // Field access
        MonoClassField* GetField(MonoClass* klass, const char* name);
        void* GetFieldValue(MonoObject* obj, MonoClassField* field);
        void SetFieldValue(MonoObject* obj, MonoClassField* field, void* value);

        template<typename T>
        T GetFieldValue(MonoObject* obj, MonoClassField* field)
        {
            T value{};
            GetFieldValueInternal(obj, field, &value);
            return value;
        }

        template<typename T>
        void SetFieldValue(MonoObject* obj, MonoClassField* field, T value)
        {
            SetFieldValueInternal(obj, field, &value);
        }

        // Static field access
        void* GetStaticFieldValue(MonoClass* klass, MonoClassField* field);
        void SetStaticFieldValue(MonoClass* klass, MonoClassField* field, void* value);

        template<typename T>
        T GetStaticFieldValue(MonoClass* klass, MonoClassField* field)
        {
            T value{};
            GetStaticFieldValueInternal(klass, field, &value);
            return value;
        }

        // Property access
        MonoProperty* GetProperty(MonoClass* klass, const char* name);
        MonoObject* GetPropertyValue(MonoObject* obj, MonoProperty* prop);

        // Method invocation
        MonoObject* InvokeMethod(MonoMethod* method, void* obj, void** params);

        // String conversion
        std::string MonoStringToUTF8(MonoString* str);
        MonoString* UTF8ToMonoString(const char* str);

        // Array access
        uintptr_t GetArrayLength(MonoArray* arr);
        void* GetArrayElement(MonoArray* arr, uintptr_t index);

        // Object utilities
        MonoClass* GetObjectClass(MonoObject* obj);
        void* Unbox(MonoObject* obj);

        // Thread attachment (required for calling Mono from native threads)
        MonoThread* AttachThread();
        void DetachThread(MonoThread* thread);

        bool IsInitialized() const { return m_initialized; }

    private:
        Mono() = default;
        ~Mono() = default;

        Mono(const Mono&) = delete;
        Mono& operator=(const Mono&) = delete;

        bool LoadMonoFunctions();
        void GetFieldValueInternal(MonoObject* obj, MonoClassField* field, void* value);
        void SetFieldValueInternal(MonoObject* obj, MonoClassField* field, void* value);
        void GetStaticFieldValueInternal(MonoClass* klass, MonoClassField* field, void* value);

        MonoDomain* m_rootDomain = nullptr;
        HMODULE m_monoModule = nullptr;
        bool m_initialized = false;

    public:
        // These need to be accessible from assembly enumeration callbacks
        using mono_assembly_foreach_t = void (*)(void (*)(MonoAssembly*, void*), void*);
        using mono_assembly_get_name_t = void* (*)(MonoAssembly*);
        using mono_assembly_name_get_name_t = const char* (*)(void*);

        mono_assembly_foreach_t fn_mono_assembly_foreach = nullptr;
        mono_assembly_get_name_t fn_mono_assembly_get_name = nullptr;
        mono_assembly_name_get_name_t fn_mono_assembly_name_get_name = nullptr;

    private:
        // Mono API function pointers
        using mono_get_root_domain_t = MonoDomain* (*)();
        using mono_domain_assembly_open_t = MonoAssembly* (*)(MonoDomain*, const char*);
        using mono_assembly_get_image_t = MonoImage* (*)(MonoAssembly*);
        using mono_class_from_name_t = MonoClass* (*)(MonoImage*, const char*, const char*);
        using mono_class_get_method_from_name_t = MonoMethod* (*)(MonoClass*, const char*, int);
        using mono_class_get_field_from_name_t = MonoClassField* (*)(MonoClass*, const char*);
        using mono_class_get_property_from_name_t = MonoProperty* (*)(MonoClass*, const char*);
        using mono_field_get_value_t = void (*)(MonoObject*, MonoClassField*, void*);
        using mono_field_set_value_t = void (*)(MonoObject*, MonoClassField*, void*);
        using mono_field_static_get_value_t = void (*)(MonoClass*, MonoClassField*, void*);
        using mono_field_static_set_value_t = void (*)(MonoClass*, MonoClassField*, void*);
        using mono_property_get_get_method_t = MonoMethod* (*)(MonoProperty*);
        using mono_runtime_invoke_t = MonoObject* (*)(MonoMethod*, void*, void**, MonoObject**);
        using mono_string_to_utf8_t = char* (*)(MonoString*);
        using mono_string_new_t = MonoString* (*)(MonoDomain*, const char*);
        using mono_array_length_t = uintptr_t (*)(MonoArray*);
        using mono_array_addr_with_size_t = char* (*)(MonoArray*, int, uintptr_t);
        using mono_object_get_class_t = MonoClass* (*)(MonoObject*);
        using mono_object_unbox_t = void* (*)(MonoObject*);
        using mono_thread_attach_t = MonoThread* (*)(MonoDomain*);
        using mono_thread_detach_t = void (*)(MonoThread*);
        using mono_free_t = void (*)(void*);
        using mono_domain_get_t = MonoDomain* (*)();
        using mono_class_get_fields_t = MonoClassField* (*)(MonoClass*, void**);
        using mono_field_get_name_t = const char* (*)(MonoClassField*);
        using mono_field_get_offset_t = uint32_t (*)(MonoClassField*);

        mono_get_root_domain_t fn_mono_get_root_domain = nullptr;
        mono_domain_assembly_open_t fn_mono_domain_assembly_open = nullptr;
        mono_assembly_get_image_t fn_mono_assembly_get_image = nullptr;
        mono_class_from_name_t fn_mono_class_from_name = nullptr;
        mono_class_get_method_from_name_t fn_mono_class_get_method_from_name = nullptr;
        mono_class_get_field_from_name_t fn_mono_class_get_field_from_name = nullptr;
        mono_class_get_property_from_name_t fn_mono_class_get_property_from_name = nullptr;
        mono_field_get_value_t fn_mono_field_get_value = nullptr;
        mono_field_set_value_t fn_mono_field_set_value = nullptr;
        mono_field_static_get_value_t fn_mono_field_static_get_value = nullptr;
        mono_field_static_set_value_t fn_mono_field_static_set_value = nullptr;
        mono_property_get_get_method_t fn_mono_property_get_get_method = nullptr;
        mono_runtime_invoke_t fn_mono_runtime_invoke = nullptr;
        mono_string_to_utf8_t fn_mono_string_to_utf8 = nullptr;
        mono_string_new_t fn_mono_string_new = nullptr;
        mono_array_length_t fn_mono_array_length = nullptr;
        mono_array_addr_with_size_t fn_mono_array_addr_with_size = nullptr;
        mono_object_get_class_t fn_mono_object_get_class = nullptr;
        mono_object_unbox_t fn_mono_object_unbox = nullptr;
        mono_thread_attach_t fn_mono_thread_attach = nullptr;
        mono_thread_detach_t fn_mono_thread_detach = nullptr;
        mono_free_t fn_mono_free = nullptr;
        mono_domain_get_t fn_mono_domain_get = nullptr;
        mono_class_get_fields_t fn_mono_class_get_fields = nullptr;
        mono_field_get_name_t fn_mono_field_get_name = nullptr;
        mono_field_get_offset_t fn_mono_field_get_offset = nullptr;
    };
}
