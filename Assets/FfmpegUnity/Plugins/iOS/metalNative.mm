#import <Foundation/Foundation.h>
//#import <Photos/Photos.h>
#import <Metal/Metal.h>

#include "IUnityInterface.h"
#include "IUnityGraphics.h"
//#include "IUnityGraphicsMetal.h"

//#include <math.h>
//#include <stdio.h>
#include <string>
//#include <assert.h>

typedef struct {
    id<MTLTexture> texture;
    uint8_t* pBytes = NULL;
} MetalData;

static MetalData* g_metalDatas = NULL;
static int g_metalDatasLength = 0;
static int g_metalDatasFinished = 0;

#define LOG_PRINTF printf

extern "C" int metalSetupNativeTextureRender(void* textureId)
{
    MetalData metalData;

    metalData.texture = (__bridge id<MTLTexture>)textureId;
    metalData.pBytes = new uint8_t[metalData.texture.width * metalData.texture.height * 4];

    if (g_metalDatas == NULL) {
        g_metalDatas = new MetalData[1];
    }
    else {
        MetalData* newData = new MetalData[g_metalDatasLength + 1];
        memcpy(newData, g_metalDatas, g_metalDatasLength * sizeof(MetalData));
        delete[] g_metalDatas;
        g_metalDatas = newData;
    }
    g_metalDatas[g_metalDatasLength] = metalData;
    g_metalDatasLength++;

    return g_metalDatasLength - 1;
}

extern "C" void metalCopyBuffer(int eventId, const uint8_t* buf)
{
    memcpy(g_metalDatas[eventId].pBytes, buf, g_metalDatas[eventId].texture.width * g_metalDatas[eventId].texture.height * 4 * sizeof(uint8_t));
}

extern "C" void metalFinishNativeTextureRender(int eventId)
{
    if (g_metalDatas[eventId].pBytes != NULL)
    {
        delete[] g_metalDatas[eventId].pBytes;
    }
    g_metalDatas[eventId].pBytes = NULL;
    g_metalDatasFinished++;

    if (g_metalDatasLength <= g_metalDatasFinished) {
        delete[] g_metalDatas;
        g_metalDatas = NULL;
        g_metalDatasLength = 0;
        g_metalDatasFinished = 0;
    }
}

static void UNITY_INTERFACE_API
metalOnRenderEvent(int eventID)
{
    MTLRegion region = MTLRegionMake2D(0, 0, g_metalDatas[eventID].texture.width, g_metalDatas[eventID].texture.height);
    [g_metalDatas[eventID].texture replaceRegion:region mipmapLevel:0 withBytes:g_metalDatas[eventID].pBytes bytesPerRow:(4 * g_metalDatas[eventID].texture.width)];
}

extern "C" UnityRenderingEvent UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
metalGetRenderEventFunc()
{
    return metalOnRenderEvent;
}