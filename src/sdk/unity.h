#pragma once

#include <cstdint>

// Unity/IL2CPP type definitions
// These will be populated with actual game structures

namespace sdk
{
    // Forward declarations
    struct Vector3;
    struct Quaternion;
    struct Transform;
    struct GameObject;
    struct Component;
    struct Camera;

    // Basic Unity types
    struct Vector3
    {
        float x, y, z;

        Vector3() : x(0), y(0), z(0) {}
        Vector3(float x, float y, float z) : x(x), y(y), z(z) {}

        Vector3 operator+(const Vector3& other) const { return Vector3(x + other.x, y + other.y, z + other.z); }
        Vector3 operator-(const Vector3& other) const { return Vector3(x - other.x, y - other.y, z - other.z); }
        Vector3 operator*(float scalar) const { return Vector3(x * scalar, y * scalar, z * scalar); }

        float Length() const;
        float Distance(const Vector3& other) const;
        Vector3 Normalized() const;
    };

    struct Vector2
    {
        float x, y;

        Vector2() : x(0), y(0) {}
        Vector2(float x, float y) : x(x), y(y) {}
    };

    struct Quaternion
    {
        float x, y, z, w;

        Quaternion() : x(0), y(0), z(0), w(1) {}
        Quaternion(float x, float y, float z, float w) : x(x), y(y), z(z), w(w) {}
    };

    struct Color
    {
        float r, g, b, a;

        Color() : r(1), g(1), b(1), a(1) {}
        Color(float r, float g, float b, float a = 1.0f) : r(r), g(g), b(b), a(a) {}

        static Color Red() { return Color(1, 0, 0); }
        static Color Green() { return Color(0, 1, 0); }
        static Color Blue() { return Color(0, 0, 1); }
        static Color Yellow() { return Color(1, 1, 0); }
        static Color White() { return Color(1, 1, 1); }
    };

    // Matrix for world-to-screen calculations
    struct Matrix4x4
    {
        float m[4][4];

        Vector3 MultiplyPoint3x4(const Vector3& point) const;
    };
}
