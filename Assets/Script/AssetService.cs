using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 统一的资源加载服务。负责 Addressables 加载、缓存、释放。
/// 对外只暴露“按 key 拿 Sprite”的接口，不暴露 Addressables 细节。
/// </summary>
public class AssetService : MonoBehaviour
{
    public static AssetService Instance { get; private set; }

    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, AsyncOperationHandle<Sprite>> handleCache
        = new Dictionary<string, AsyncOperationHandle<Sprite>>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        ReleaseAll();
    }

    /// <summary>
    /// 异步加载一个 Sprite。命中缓存则同步回调，否则等待 Addressables 完成。
    /// </summary>
    public IEnumerator LoadSprite(string key, Action<Sprite> onDone)
    {
        if (string.IsNullOrEmpty(key))
        {
            onDone?.Invoke(null);
            yield break;
        }

        // 命中缓存
        if (spriteCache.TryGetValue(key, out var cached))
        {
            onDone?.Invoke(cached);
            yield break;
        }

        // 已发起过就复用句柄，避免重复请求
        if (!handleCache.TryGetValue(key, out var handle))
        {
            handle = Addressables.LoadAssetAsync<Sprite>(key);
            handleCache[key] = handle;
        }

        while (!handle.IsDone) yield return null;

        if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null)
        {
            spriteCache[key] = handle.Result;
            onDone?.Invoke(handle.Result);
        }
        else
        {
            Debug.LogWarning($"[AssetService] 加载失败: {key}");
            onDone?.Invoke(null);
        }
    }

    /// <summary>释放某个 key 的资源。如果还想用，需要重新加载。</summary>
    public void Release(string key)
    {
        spriteCache.Remove(key);
        if (handleCache.TryGetValue(key, out var h))
        {
            if (h.IsValid()) Addressables.Release(h);
            handleCache.Remove(key);
        }
    }

    /// <summary>释放所有缓存资源。</summary>
    public void ReleaseAll()
    {
        foreach (var h in handleCache.Values)
        {
            if (h.IsValid()) Addressables.Release(h);
        }
        handleCache.Clear();
        spriteCache.Clear();
    }
}