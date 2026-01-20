using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PublicOpinionManager : MonoBehaviour
{
    [SerializeField,Header("表示上の世論の数値")]
    [Range(-1,1)]private float PublicOpinion;

    [Header("UI参照")]
    [SerializeField] private Image faceImage; // 顔を表示するImageコンポーネント
    [SerializeField] private TextMeshProUGUI text; // 世論値をパーセントで表示するtextコンポーネント

    [Header("白いスプライト画像")]
    [SerializeField] private Sprite sadSprite;    // 悲しい顔（白）
    [SerializeField] private Sprite normalSprite; // 普通の顔（白）
    [SerializeField] private Sprite smileSprite;  // 笑顔（白）

    [Header("色の設定")]
    [SerializeField] private Color badColor = Color.red;    // -1に近いときの色（赤）
    [SerializeField] private Color midColor = Color.yellow; // 0付近の色（黄）
    [SerializeField] private Color goodColor = Color.green; // 1に近いときの色（緑）

    public float POchangeSpeed = 1f;

    [Range(-1, 1)] public float POvalue;   //値を参照するときはこれを参照して下さい
    public static PublicOpinionManager Instance { get; private set; }

    private Vector3 pos;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            
        }
        Instance = this;
    }

    private void Start()
    {
        //pos = faceImage.rectTransform.anchoredPosition;
    }

    private void Update()
    {
        // null チェックを追加
        if (faceImage == null || text == null)
        {
            Debug.LogWarning("PublicOpinionManager: UI参照が設定されていません");
            return;
        }

        PublicOpinion = Mathf.Lerp(PublicOpinion,POvalue,POchangeSpeed * Time.deltaTime);
        //faceImage.rectTransform.anchoredPosition = new Vector3(pos.x,pos.y + PublicOpinion * 100,pos.z);
        faceColor();
        faceSplite();
        Text();
    }

    private void Text()
    {
        if (text == null) return;
        
        int percent;
        float normalizedValue = (PublicOpinion + 1f) / 2f;

        // 0 ~ 1 を 0 ~ 100% に変換
        percent = (int)((normalizedValue * 100f) + 0.1f);
        text.text = "支持率:" + percent + "％";
    }

    private void faceColor()
    {
        if (faceImage == null || text == null) return; // null チェック追加
        
        Color currentColor;
        Color currentColor_text;
        if (POvalue < 0)
        {
            // マイナスエリア (-1 ～ 0)
            // -1(赤) から 0(黄) へ変化させる
            // t + 1 をすることで、入力「-1～0」を「0～1」の割合に変換できる
            currentColor = Color.Lerp(badColor, midColor, PublicOpinion + 1f);
            currentColor_text = Color.Lerp(badColor, midColor, PublicOpinion + 1f);
        }
        else
        {
            // プラスエリア (0 ～ 1)
            // 0(黄) から 1(緑) へ変化させる
            // t はそのまま「0～1」の割合として使える
            currentColor = Color.Lerp(midColor, goodColor, PublicOpinion);
            currentColor_text = Color.Lerp(midColor, goodColor, PublicOpinion);
        }

        // 色を適用
        faceImage.color = currentColor;
        text.color = currentColor_text;
    }

    private void faceSplite()
    {
        if (faceImage == null) return;
        
        if (PublicOpinion >= 0.35 && faceImage.sprite != smileSprite)
            faceImage.sprite = smileSprite;
        else if(PublicOpinion <= -0.35 && faceImage.sprite != sadSprite)
            faceImage.sprite = sadSprite;
        else if(PublicOpinion < 0.35 && PublicOpinion > -0.35 && faceImage.sprite != normalSprite)
            faceImage.sprite = normalSprite;
    }


    /// <summary>
    /// 世論の変化。引数は増減させる量(-2f~+2f)。世論の範囲は-1f~+1fとする。
    /// </summary>
    /// <param name="value">ここに記入した値だけ変化する</param>
    public void POchanging(float value)
    {
        value = Mathf.Clamp(value,-2,2);
        POvalue += value;
        POvalue = Mathf.Clamp(POvalue,-1,1);
    }

    /// <summary>
    /// 現在のPO値が任意の引数以上かかどうか調べる。
    /// </summary>
    /// <param name="judgementValue">判断基準となる値</param>
    /// <returns>引数以上であればtrue、そうでなければfalseを返す。</returns>
    public bool JudgePO(float judgementValue)
    {
        return POvalue >= judgementValue;
    }

    /// <summary>
    /// 世論値をパーセント（0〜100）で取得する
    /// </summary>
    /// <returns>0〜100のパーセント値</returns>
    public int GetPOpercent()
    {
        float normalizedValue = (POvalue + 1f) / 2f;
        return (int)((normalizedValue * 100f) + 0.1f);
    }

}