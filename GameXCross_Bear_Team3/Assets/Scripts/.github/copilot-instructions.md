# GameXCross - Copilot Instructions

## 🎮 プロジェクト概要

**GameXCross** はタワーディフェンス形式の 3D ゲーム。プレイヤーはクマの襲撃から家を守り、ハンターや罠でディフェンスします。

- **エンジン**: Unity
- **言語**: C#
- **重要なフレームワーク**: UniRx（リアクティブプログラミング）、DOTween（アニメーション）

---

## 📁 ファイル構成

```
Assets/Scripts/
├── System/           # ゲーム管理・UI
│   ├── GameManager.cs           # ゲーム全体の状態管理（Setup/Battle/Result）
│   ├── GameBalanceData.cs       # 全パラメータをScriptableObjectで一元管理
│   ├── money.cs                 # 経済システム
│   ├── PauseManager.cs          # ポーズ機能
│   ├── CameraController.cs      # カメラ制御
│   └── その他UI制御
├── Bear/             # クマAI
│   ├── BearController.cs        # クマの攻撃AI・状態管理
│   └── BearSpawner.cs           # Addressablesでクマを非同期生成
├── Hunter/           # ハンターAI
│   └── HunterController.cs      # ハンターの自動防衛・パトロール
├── Trap/             # 防衛システム
│   ├── CageTrap.cs              # 檻（クマを捕獲）
│   ├── ElectricFence.cs         # 電気柵（クマを麻痺）
│   ├── Fence.cs                 # 壁（通常の障害物）
│   ├── TrapTarget.cs            # インターフェース（ApplyStun, Capture）
│   ├── TreeController.cs        # 障害物
│   ├── HouseHealth.cs           # 家のHP管理
│   └── PointerContoroller.cs    # UI選択・ポインタ制御
└── BGM,SE/           # サウンド
    ├── BGMManager.cs
    └── SoundManager.cs
```

---

## 🎯 ゲームロジック・フロー

### **3 つのフェーズ**

| フェーズ   | 説明                               | 時間                |
| ---------- | ---------------------------------- | ------------------- |
| **Setup**  | プレイヤーが罠・ディフェンスを配置 | 30 秒（デフォルト） |
| **Battle** | クマが波状で出現、自動防衛開始     | 制限なし            |
| **Result** | ウェーブ完了またはゲームオーバー   | -                   |

### **ウェーブシステム**

- `GameBalanceData.maxWaves`で最大ウェーブ数を管理
- `GameBalanceData.enemiesPerWave[]`でウェーブごとの敵数を指定
- 全敵を倒/捕獲で次のセットアップへ
- 家が全て破壊されたらゲームオーバー

---

## 🔑 重要な設計パターン

### **1. UniRx を使った状態管理**

```csharp
// GameManager内
public ReactiveProperty<GameState> CurrentState { get; private set; }
public ReactiveProperty<float> TimeRemaining { get; private set; }
public ReactiveProperty<int> CurrentWave { get; private set; }

// 購読例
GameManager.Instance.CurrentState
    .Subscribe(state => /* 状態変更時の処理 */)
    .AddTo(this);
```

**重要**: 全ての`.Subscribe()`には`.AddTo(this)`または`.AddTo(_disposables)`で購読解除を管理すること（メモリリーク防止）。

### **2. TrapTarget インターフェース**

罠がクマに効果を与えるための統一インターフェース：

```csharp
public interface TrapTarget
{
    void ApplyStun(float duration, float damage);  // ElectricFence用
    void Capture(GameObject trapObject);           // CageTrap用
}
```

BearController はこれを実装。

### **3. NavMeshAgent の制御**

- **BearController**: 敵移動・追跡
- **HunterController**: パトロール・攻撃位置調整
- `isStopped = true`で一時停止、`SetDestination()`で移動指示

### **4. Addressables での非同期生成**

BearSpawner.cs で非同期にプレハブをロード・生成。失敗時にカウント調整。

---

## 🐛 既知の問題と修正状況

### **✅ 修正完了（2026/01/09）**

#### **P1: CageTrap 複数捕獲対応**

- **問題**: `.First()`で最初の 1 体だけ捕獲、2 体目以降をスルー
- **修正**: `.First()`削除 → 複数体捕獲可能に
- **ファイル**: `Trap/CageTrap.cs`

#### **P1: 報酬二重支払い防止**

- **問題**: 感電 → 回復 → 死亡で報酬が 2 回支払われる
- **修正**: `Capture()`で報酬確定、`_isDead = true`設定
- **ファイル**: `Bear/BearController.cs`

#### **P1: ウェーブ終了判定の混乱**

- **問題**: `IsParalyzed`と`IsDead`の混在で stuck 状態に陥る
- **修正**: 判定を`!e.IsDead`のみに統一、捕獲を Dead 扱い
- **ファイル**: `System/GameManager.cs`

### **⚠️ 残る中程度リスク（P2）**

#### **P2: HouseHealth.Collapse()の二重呼び出し**

- **問題**: 同フレーム内複数ダメージで複数回実行
- **対策案**: `_isCollapsing`フラグで一度だけ実行
- **優先度**: 中（視覚的な問題、ゲーム進行に影響なし）

#### **P2: NavMeshAgent 復帰時の位置ずれ**

- **問題**: 無効化後の復帰で無効な位置から再開
- **対策案**: 位置をセーフティに保存して復帰時に復元
- **優先度**: 中（稀な条件でのバグ）

---

## 📋 コーディング規約

### **命名規則**

- **プライベート変数**: `_camelCase` （例: `_targetHouse`）
- **パブリック変数**: `PascalCase` （例: `CurrentState`）
- **ReactiveProperty**: `PascalCase` （例: `CurrentWave`）
- **メソッド**: `PascalCase` （例: `RegisterEnemy()`, `TakeDamage()`)

### **UniRx の使い方**

```csharp
// ✅ 正しい
this.OnTriggerEnterAsObservable()
    .Subscribe(other => { /* 処理 */ })
    .AddTo(this);  // 必須：購読解除管理

// ❌ 避ける
this.OnTriggerEnterAsObservable()
    .Subscribe(other => { /* 処理 */ });  // メモリリーク
```

### **NavMeshAgent 制御**

```csharp
// 移動停止時
_agent.isStopped = true;
_agent.velocity = Vector3.zero;

// 移動再開時
if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
{
    _agent.isStopped = false;
}

// 無効化
_agent.enabled = false;
```

### **DOTween アニメーション**

```csharp
// SetLink()でGameObject破棄時に自動停止
transform.DOScale(Vector3.zero, 0.5f).SetLink(gameObject);

// Sequence使用時は OnComplete で破棄
DOTween.Sequence()
    .Append(transform.DOMove(...))
    .OnComplete(() => Destroy(gameObject));
```

---

## 🔧 テスト・確認方法

### **クマの動作確認**

1. **Setup**: クマが待機しているか確認
2. **Battle 開始**: クマが家に向かうか確認
3. **罠テスト**:
  - 檻：クマが捕獲されるか、報酬が支払われるか
  - 電気柵：クマが麻痺し、一定時間後に回復するか
4. **ハンター検出**: ハンターが出現したらターゲットを切り替えるか

### **ウェーブシステム確認**

1. 全敵倒/捕獲 → Next Wave（セットアップ開始）
2. 家破壊 → Result Scene へ遷移
3. 最終ウェーブ完了 → Clear 画面表示

### **バランスデータ変更**

`GameBalanceData` ScriptableObject でパラメータを一元管理。
プレイ中に Editor 上で変更可能（デバッグモード時）。

---

## 🚀 コード追加時のチェックリスト

- [ ] UniRx の購読に`.AddTo()`を付けた
- [ ] NavMeshAgent 操作で`isActiveAndEnabled`をチェック
- [ ] TrapTarget が関わる機能なら`Capture()`と`ApplyStun()`の両方実装
- [ ] GameManager に敵・家の登録/報告は済んだか
- [ ] DOTween アニメーションに`.SetLink(gameObject)`か`OnComplete`は付けた
- [ ] デバッグログで状態遷移を記録した（ウェーブ開始、敵倒、家破壊など）

---

## 📞 重要なメソッド・イベント

### **GameManager（中枢）**

```csharp
// 敵・家の登録
void RegisterEnemy(BearController bear)
void RegisterHouse(HouseHealth house)

// 敵・家の報告（状態管理）
void ReportEnemyDefeated(BearController bear)
void ReportHouseDestroyed(HouseHealth house)

// 報酬
void AddPendingReward(int amount)  // ウェーブ終了時に支払い

// フェーズ遷移
void StartSetupPhase()
void StartBattlePhase()
void TransitionToResultScene(bool isClear)
```

### **BearController（敵 AI）**

```csharp
// 初期化
void Initialize(float speed)

// ダメージ・状態変化
void TakeDamage(float damage, HunterController attacker)
void ApplyStun(float duration, float damage)      // TrapTarget実装
void Capture(GameObject trap)                      // TrapTarget実装

// AI制御
void FindNextTarget()
void DetectNearestHunter()
void HandleHunterTarget() / HandleHouseTarget()
```

### **HunterController（自動防衛）**

```csharp
// 攻撃
void Attack(BearController target)

// 状態管理
void StopMovement()
void ReturnToWait()

// パトロール
void PatrolRandomly()
```

---

## 💡 よくある落とし穴

1. **`.AddTo()`忘れ**: メモリリーク → ガベージが蓄積
2. **NavMeshAgent 無効化忘れ**: Destroy したのに物理判定が残る
3. **報酬二重支払い**: 複数の場所で支払い処理
4. **ウェーブ stuck**: 敵生成失敗時のカウント調整が不完全
5. **敵が家を通り過ぎる**: ClosestPoint 計算の遊び値設定

---

## 📚 参考資料

- **UniRx**: https://github.com/neuecc/UniRx
- **DOTween**: http://dotween.demigiant.com/
- **Unity NavMesh**: https://docs.unity3d.com/Manual/nav-NavigationSystem.html
- **Addressables**: https://docs.unity3d.com/Packages/com.unity.addressables@latest
