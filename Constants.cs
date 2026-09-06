using System;
using Unity.Netcode;
using UnityEngine;

public static class Constants
{
    public const float Gravity = -9.81f;

    public struct Coord: INetworkSerializable, IEquatable<Coord>
    {
        public int R, C;

        public Coord(int R, int C)
        {
            this.R = R;
            this.C = C;
        }


        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref R);
            serializer.SerializeValue(ref C);
        }
        public bool Equals(Coord other)
        {
            return R == other.R && C == other.C;
        }
    }

    public const int MAPSIZE = 20;

    public const int MAXLIFE = 2;
    public const int MAXBOMBCOUNT= 5;
    public const int MAXRANGE = 10;
    public const float MAXSPEED = 20;

    public enum CharacterType
    {
        Count,
        Range,
        Speed
    }

    public enum EnemyType
    {
        runner = 3,
        bomber = 7
    }

    public struct EnemySpawnInfo
    {
        public EnemyType enemyType;
        public Coord coord;

        public EnemySpawnInfo(EnemyType enemyType, Coord coord)
        {
            this.enemyType = enemyType;
            this.coord = coord;
        }
    }

    public const string FIREBASEURL = "https://threebombers-2a858-default-rtdb.firebaseio.com/";

}