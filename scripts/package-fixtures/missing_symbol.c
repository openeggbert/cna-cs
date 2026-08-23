// SPDX-License-Identifier: MIT

#include <stdint.h>

#if defined(_WIN32)
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

EXPORT uint32_t cna_get_abi_version(void) { return UINT32_C(6) << 8; }
