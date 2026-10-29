using System.Numerics;
using System.Collections;
using System.Runtime.InteropServices;

namespace ArenaGame.thingies;

public partial interface Thingy<Self>
    where Self : struct, Thingy<Self>;

public static class Thingy {
    public interface Ticking<Self> : Thingy<Self>
        where Self : struct, Ticking<Self>
    {
        public static abstract void Tick(ref Self self, float dt, Action<Self>? addMore, out bool removeSelf);
    }

    public static class Ticking {
        public interface Immutable<Self> : Ticking<Self>
            where Self : struct, Immutable<Self>
        {
            public static abstract void Tick(ref Self self, float dt);

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
        public static abstract void Render(Self self, float dt);
    }
}

public partial interface Thingy<Self> {
    public readonly struct Manager() : IEnumerable<Self> {
        public readonly List<Self> thingies = [];
        
        private static void SwapRemove<T>(List<T> list, int index) {
            int last = list.Count - 1;
            list[index] = list[last];
            list.RemoveAt(last);
        }

        public static void Tick<Ticking>(Thingy<Ticking>.Manager self, float dt)
            where Ticking : struct, Thingy.Ticking<Ticking>, Thingy<Self>
        {
            List<Ticking> addBuf = [];
            List<int> remBuf = [];
    
            var span = CollectionsMarshal.AsSpan(self.thingies);
            for (int i = 0; i < span.Length; i++) {
                ref var thingy = ref span[i];
                Ticking.Tick(ref thingy, dt, addBuf.Add, out var removeSelf);
                if (removeSelf) remBuf.Add(i);
            }
            for (int i = remBuf.Count(); i --> 0;) {
                SwapRemove(self.thingies, remBuf[i]);
            }
            foreach (var thingy in addBuf) {
                self.thingies.Add(thingy);
            }
        }

        public static void Render<Rendering>(Thingy<Rendering>.Manager self, float dt)
            where Rendering : struct, Thingy.Rendering<Rendering>, Thingy<Self>
        {
            foreach (var thingy in self.thingies) Rendering.Render(thingy, dt);
        }

        // sugars
        public Self this[int index] => this.thingies[index];
        public int Count() => this.thingies.Count();

        IEnumerator<Self> IEnumerable<Self>.GetEnumerator() => this.thingies.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.thingies.GetEnumerator();

        public readonly void Clear() {
            this.thingies.Clear();
        }
        public readonly void Add(Self thingy) {
            this.thingies.Add(thingy);
        }
        public readonly void RemoveAt(int i) {
            this.thingies.RemoveAt(i);
        }
    }
}
