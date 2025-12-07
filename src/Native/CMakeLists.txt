cmake_minimum_required(VERSION 3.20)
project(LethalMenuBootstrap)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Create shared library (DLL)
add_library(LethalMenuBootstrap SHARED Bootstrap.cpp)

# Set output directory
set_target_properties(LethalMenuBootstrap PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_SOURCE_DIR}/../../bin"
    RUNTIME_OUTPUT_DIRECTORY_DEBUG "${CMAKE_SOURCE_DIR}/../../bin"
    RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_SOURCE_DIR}/../../bin"
    OUTPUT_NAME "LethalMenuBootstrap"
)

# No additional libraries needed - uses only Windows API
