#include "pch.h"
#include "unity.h"
#include <cmath>

namespace sdk
{
    float Vector3::Length() const
    {
        return std::sqrt(x * x + y * y + z * z);
    }

    float Vector3::Distance(const Vector3& other) const
    {
        return (*this - other).Length();
    }

    Vector3 Vector3::Normalized() const
    {
        float len = Length();
        if (len == 0)
            return Vector3();
        return Vector3(x / len, y / len, z / len);
    }

    Vector3 Matrix4x4::MultiplyPoint3x4(const Vector3& point) const
    {
        Vector3 result;
        result.x = m[0][0] * point.x + m[0][1] * point.y + m[0][2] * point.z + m[0][3];
        result.y = m[1][0] * point.x + m[1][1] * point.y + m[1][2] * point.z + m[1][3];
        result.z = m[2][0] * point.x + m[2][1] * point.y + m[2][2] * point.z + m[2][3];
        return result;
    }
}
