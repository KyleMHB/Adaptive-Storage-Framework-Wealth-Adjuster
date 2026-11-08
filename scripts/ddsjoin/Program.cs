// usage: ddsjoin <Waylith.dll> <in.split> <out.dds> [<in.split> <out.dds>...]
using System.Reflection;
using System.Runtime.Loader;

var waylith = Assembly.LoadFrom(args[0]);
AssemblyLoadContext.Default.Resolving += (ctx, name) =>
{
    try { return ctx.LoadFromAssemblyName(new AssemblyName(name.Name)); } catch { return null; }
};
var join = waylith.GetType("Waylith.Graphics.Textures.DdsFile", true).GetMethod("TryJoin", BindingFlags.Public | BindingFlags.Static);
var trampoline = typeof(Joiner).GetMethod(nameof(Joiner.Run))!;
for (int i = 1; i + 1 < args.Length; i += 2)
{
    var data = File.ReadAllBytes(args[i]);
    var result = Joiner.Invoke(join, data);
    if (result == null) { Console.Error.WriteLine("join failed: " + args[i]); Environment.Exit(1); }
    File.WriteAllBytes(args[i + 1], result);
}

static class Joiner
{
    public delegate bool JoinFn(ReadOnlySpan<byte> split, out byte[] joined, out int length);
    public static void Run() { }
    public static byte[] Invoke(MethodInfo method, byte[] data)
    {
        var fn = method.CreateDelegate<JoinFn>();
        if (!fn(data, out var joined, out var length)) return null;
        return joined.AsSpan(0, length).ToArray();
    }
}
