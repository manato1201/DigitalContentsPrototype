
using System;
using System.Collections.Generic;
using UnityEngine;


namespace DB
{

    [CreateAssetMenu(fileName = "DB_Coordinate", menuName = "Scriptable Objects/DB_Coordinate")]
    public class DB_Coordinate : ScriptableObject
    {

        #region QOL向上処理
        // CakeParamsSOが保存してある場所のパス
        public const string PATH = "DB_Coordinate";

        // CakeParamsDBの実体
        private static DB_Coordinate _entity = null;
        public static DB_Coordinate Entity
        {
            get
            {
                // 初アクセス時にロードする
                if (_entity == null)
                {
                    _entity = Resources.Load<DB_Coordinate>(PATH);

                    //ロード出来なかった場合はエラーログを表示
                    if (_entity == null)
                    {
                        Debug.LogError(PATH + " not found");
                    }
                }

                return _entity;
            }
        }
        #endregion

        [Header("アニメーション・画像")] public List<NameSprite> ItemSprites;

        [Header("座標")] public List<NameCoordinate> Coordinates;
    }
    [Serializable]
    public class NameSprite
    {
        public string Name;
        public Sprite[] Sprite;
    }
    [Serializable]
    public class NameCoordinate
    {
        public string Name;
        public float[] XCoordinate;
        public float[] YCoordinate;
    }
}