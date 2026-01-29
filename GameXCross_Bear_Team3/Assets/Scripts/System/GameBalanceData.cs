using UnityEngine;

// [CreateAssetMenu(fileName = "GameBalanceData", menuName = "Game/GameBalanceData")]
public class GameBalanceData : ScriptableObject
{
    [Header("--- ゲーム全体設定 ---")]
    [Tooltip("準備時間の長さ（秒）")]
    public int setupTime = 30;
    [Tooltip("最大ウェーブ数")]
    public int maxWaves = 3;

    [Header("--- ウェーブ設定 (敵の数) ---")]
    public int[] enemiesPerWave = new int[] { 1, 2, 3 };

    [Header("--- ハンター ---")]
    public float hunterMaxHealth = 50f;
    public float hunterMoveSpeed = 3.5f;
    public float hunterAttackDamage = 10f;
    public float hunterAttackRange = 15.0f;
    public float hunterAttackInterval = 1.5f;
    public float hunterDetectionRadius = 20.0f;

    [Header("--- クマ ---")]
    public float bearMaxHealth = 100f;
    public float bearMoveSpeed = 5.0f;
    public float bearAttackDamage = 20f;
    public float bearAttackInterval = 1.0f;
    public float bearHouseAttackRange = 5.0f;
    public float bearHunterAttackRange = 10.0f;
    public float bearDetectionRadius = 15.0f;

    [Header("--- 環境 (家・設備) ---")]
    public float houseMaxHp = 100f;

    [Header("--- ギミック (電気柵) ---")]
    public float fenceDamage = 10f;
    public float fenceParalysisDuration = 3.0f;

    [Header("--- 金額設定 ---")]
    [Tooltip("初期所持金")]
    public int initialMoney = 1000000;

    [Tooltip("クマ討伐時の報酬")]
    public int bearDefeatReward = 30000;
    
    [Tooltip("クマ捕獲時の報酬")]
    public int bearCaptureReward = 45000;

    /*
    [Tooltip("ハンターの雇用コスト")]
    public int hunterCost = 300000;

    [Tooltip("ケージの建設コスト")]
    public int cageCost = 100000;
    
    [Tooltip("電気柵の建設コスト")]
    public int fenceCost = 50000;
    */
}