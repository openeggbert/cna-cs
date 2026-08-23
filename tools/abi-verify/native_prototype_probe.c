// SPDX-License-Identifier: MIT

#include <stdint.h>

#include "CNA/C/cna.h"

typedef CNA_Result (*ExpectedGameCreate)(const CNA_GameCreateInfo*, CNA_Handle*);
typedef void (*ExpectedDeviceEventCallback)(CNA_Handle, void*);
typedef CNA_Result (*ExpectedDeviceSubscribe)(
    CNA_Handle, uint32_t, ExpectedDeviceEventCallback, void*, uint64_t*);
typedef CNA_Result (*ExpectedApply3DMulti)(
    CNA_Handle, const CNA_AudioListener*, uint64_t, const CNA_AudioEmitter*);
typedef CNA_Result (*ExpectedAudioEngineCreateWithRenderer)(
    CNA_Handle, CNA_StringView, int64_t, CNA_StringView, CNA_Handle*);
typedef CNA_Result (*ExpectedLifecycleCallback)(
    CNA_Handle, const CNA_GameTime*, void*, CNA_CallbackError*);

static ExpectedGameCreate check_game_create = cna_game_create;
static ExpectedDeviceSubscribe check_device_subscribe = cna_graphics_device_subscribe_event;
static ExpectedApply3DMulti check_apply_3d_multi = cna_sound_effect_instance_apply_3d_multi_ext;
static ExpectedAudioEngineCreateWithRenderer check_audio_engine = cna_audio_engine_create_with_renderer;
static ExpectedLifecycleCallback check_lifecycle_callback = (CNA_GameLifecycleCallback)0;

int main(void)
{
    return check_game_create == 0 || check_device_subscribe == 0 ||
           check_apply_3d_multi == 0 || check_audio_engine == 0 ||
           check_lifecycle_callback != 0;
}
