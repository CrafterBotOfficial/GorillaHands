using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace GorillaHands;

public class AssetLoader
{
    private AssetBundle bundle;

    public AssetLoader(string resourcePath)
    {
        using Stream assetReaderStream = typeof(AssetLoader).Assembly.GetManifestResourceStream(resourcePath);
        bundle = AssetBundle.LoadFromStream(assetReaderStream);
    }

    public Task<UnityEngine.Object> LoadAsset(string name)
    {
        var taskCompletionSource = new TaskCompletionSource<UnityEngine.Object>();
        AssetBundleRequest request = bundle.LoadAssetAsync(name);
        request.completed += operation => taskCompletionSource.SetResult(request.asset);
        return taskCompletionSource.Task;
    }
}
