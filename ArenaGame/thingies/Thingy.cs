using System.Runtime.InteropServices;

namespace ArenaGame.thingies;

public interface Thingy<Self>
    where Self : struct, Thingy<Self>
{
    static Thingy() {
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(Self).TypeHandle);
    }

    static List<Self> List { get; set; } = [];
    static virtual void HandleReset(out bool shouldClear) {
        shouldClear = true;
    }
}

public static class Thingy {
    public interface Ticking<Self> : Thingy<Self>
        where Self : struct, Ticking<Self>
    {
        static abstract void Tick(ref Self self, float dt, Action<Self>? addMore, out bool removeSelf);
    }

    public static class Ticking {
        public interface Immutable<Self> : Ticking<Self>
            where Self : struct, Immutable<Self>
        {
            static abstract void Tick(ref Self self, float dt);

            static void Ticking<Self>
                .Tick(ref Self self, float dt, Action<Self>? addMore, out bool removeSelf) 
            {
                removeSelf = false;
                Self.Tick(ref self, dt);
            }
        }
    }
    
    public interface Rendering<Self> : Thingy<Self>
        where Self : struct, Rendering<Self>
    {
        static abstract void Render(Self self, float dt);
    }

    extension<T>(T) where T : struct, Thingy<T>
    {
        public static Span<T> AsSpan() => CollectionsMarshal.AsSpan(Thingy<T>.List);
        public static Span<T> Span => AsSpan<T>();
    
        public static ref T Val { get {
            if (Thingy<T>.List.Count == 0) Thingy<T>.List.Add(default);
            return ref AsSpan<T>()[0];
        }}

        public static void Reset() 
        {
            T.HandleReset(out bool shouldClear);
            if (shouldClear) Thingy<T>.List.Clear();
        }
    
        public static void Add(T thingy) 
        {
            Thingy<T>.List.Add(thingy);
        }
        public static void RemoveAt(int index)
        {
            int last = Thingy<T>.List.Count - 1;
            Thingy<T>.List[index] = Thingy<T>.List[last];
            Thingy<T>.List.RemoveAt(last);
        }
    }

    extension<T>(T) where T : struct, Ticking<T>
    {
        public static void TickAll(float dt)
        {
            List<T> addBuf = [];
            List<int> remBuf = [];

            var span = AsSpan<T>();
            for (int i = 0; i < span.Length; i++) {
                ref var thingy = ref span[i];
                T.Tick(ref thingy, dt, addBuf.Add, out var removeSelf);
                if (removeSelf) remBuf.Add(i);
            }
            for (int i = remBuf.Count; i --> 0;) RemoveAt<T>(remBuf[i]);
            foreach (var thingy in addBuf) Add(thingy);
        }
    }
    extension<T>(T) where T : struct, Rendering<T>
    {
        public static void RenderAll(float dt)
        {
            foreach (var thingy in AsSpan<T>()) T.Render(thingy, dt);
        }
    }
}
