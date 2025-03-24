using System;
using System.Collections.Generic;
using System.IO;

namespace GameClient.Tetris; 

public struct MinoBag : IStateSerializable {
    // Splits ulong(64bits) into 21 segments of 3-bit containers, each containing 0 for none, 1~7 for their corresponding minos
    private ulong bag;
    private int nextSeed;
    
    public int Count { get; private set; }
    
    public int Next => (int) (bag & 0b111u) - 1;

    public MinoBag() {
        bag = 0;
        Count = 0;
        nextSeed = Random.Shared.Next();
    }
    
    public MinoBag(int initialSeed) {
        bag = 0;
        Count = 0;
        nextSeed = initialSeed;
    }

    public MinoBag(IEnumerable<ulong> minos) : this() {
        foreach (ulong mino in minos) AddMino(mino + 1);
    }

    public int Pop() {
        while (Count < Constants.MinoCount * 2) GenerateNextBag();
        int nextMino = Next;
        bag >>= 3;
        Count--;

        return nextMino;
    }

    private void GenerateNextBag() {
        Random random = new Random(nextSeed);
        nextSeed = random.Next();
        Span<uint> nextBag = stackalloc[] { 1u, 2u, 3u, 4u, 5u, 6u, 7u };
        for (int i = 6; i > 0; i--) {
            int swap = random.Next(i + 1);
            (nextBag[i], nextBag[swap]) = (nextBag[swap], nextBag[i]);
            AddMino(nextBag[i]);
        }

        AddMino(nextBag[0]);
    }

    private void AddMino(ulong minoType) {
        bag |= minoType << (Count * 3);
        Count++;
    }

    public int this[int i] => (int) ((bag >> (i * 3)) & 0b111ul) - 1;

    public bool Equals(MinoBag other) {
        return bag == other.bag && nextSeed == other.nextSeed;
    }

    public override bool Equals(object obj) {
        return obj is MinoBag other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(bag, nextSeed);
    }

    public static bool operator ==(MinoBag left, MinoBag right) {
        return left.Equals(right);
    }

    public static bool operator !=(MinoBag left, MinoBag right) {
        return !(left == right);
    }

    public void Serialize(BinaryWriter writer) {
        writer.Write(bag);
        writer.Write(nextSeed);
        writer.Write(Count);
    }

    public static MinoBag Deserialize(BinaryReader reader) {
        return new MinoBag {
            bag = reader.ReadUInt64(),
            nextSeed = reader.ReadInt32(),
            Count = reader.ReadInt32(),
        };
    }
}