namespace Rotationator;

// sead::Random
public class SeadRandom
{
    private readonly uint[] _state;

    public SeadRandom() : this((uint)Environment.TickCount)
    {
        
    }

    public SeadRandom(uint seed)
    {
        _state = new uint[4];
        _state[0] = 1812433253 * (seed ^ (seed >> 30)) + 1;
        _state[1] = 1812433253 * (_state[0] ^ (_state[0] >> 30)) + 2;
        _state[2] = 1812433253 * (_state[1] ^ (_state[1] >> 30)) + 3;
        _state[3] = 1812433253 * (_state[2] ^ (_state[2] >> 30)) + 4;
    }

    public SeadRandom(uint seedOne, uint seedTwo, uint seedThree, uint seedFour)
    {
        _state = new uint[] { seedOne, seedTwo, seedThree, seedFour };
    }

    public SeadRandom(uint[] context)
    {
        if (context.Length != 4)
        {
            throw new ArgumentException("Invalid context for SeadRandom");
        }
        
        _state = context;
    }

    public uint GetUInt32()
    {
        uint v1 = _state[0] ^ (_state[0] << 11);
        _state[0] = _state[1];
        uint v2 = _state[3];
        uint v3 = v1 ^ (v1 >> 8) ^ v2 ^ (v2 >> 19);
        _state[1] = _state[2];
        _state[2] = v2;
        _state[3] = v3;

        return v3;
    }

    // f32 al::getRandom()
    public float GetSingle()
    {
        uint random = (GetUInt32() >> 9) | 0x3F800000;
        return BitConverter.UInt32BitsToSingle(random) + -1.0f;
    }

    // f32 al::getRandom(f32, f32)
    public float GetSingle(float min, float max)
    {
        return (GetSingle() * (max - min)) + min;
    }

    // f32 al::getRandom(f32)
    public float GetSingle(float factor)
    {
        return GetSingle(0.0f, factor);
    }
    
    // s32 al::getRandom(s32, s32)
    public int GetInt32(int min, int max)
    {
        return (int)GetSingle(min, max);
    }

    // s32 al::getRandom(s32)
    public int GetInt32(int factor)
    {
        return GetInt32(0, factor);
    }

    public uint[] GetContext()
    {
        return _state;
    }
}