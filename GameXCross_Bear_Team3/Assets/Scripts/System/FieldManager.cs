using UnityEngine;
using System;
using System.Collections;

public class FieldManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private money money;

    [Header("資金増加間隔(s)")]
    [SerializeField] private float interval;
    [Header("各レベルの間隔当たりの資金増加量")]
    [SerializeField] private int[] value;
    [Header("各レベル時のレベルアップ必要資金")]
    [SerializeField] private int[] cost;
    [Header("各レベルの見た目")]
    [SerializeField] private GameObject[] ob;
    [Header("現在レベル")]
    [SerializeField] private int level = 0;
    [Header("最大レベル上限")]
    [SerializeField] private int MAXlevel = 9;

    [SerializeField] private GameObject efectprefab;
    [SerializeField] private GameObject efectpsition;

    private float timer;

    private void Awake()
    {
        Array.Resize(ref value, MAXlevel+1);
        Array.Resize(ref cost, MAXlevel+1);
        Array.Resize(ref ob, MAXlevel + 1);

        for (int i=1;i<MAXlevel+1;i++)//初期レベル以外全ての見た目を消しておく
        {
            ob[i].SetActive(false);
        }
    }
    private void Update()
    {
        if(gameManager.CurrentState.Value == GameState.Battle )
        {
            timer += Time.deltaTime;
            if(timer > interval/5)
            {
                money.moneycount += value[level]/5;
                timer = 0;
            }
        }
    }

    public void LvUp()
    {
        level++;
        ChangeAppearance();
        StartCoroutine(Efect());
    }

    private void ChangeAppearance()
    {
        ob[level - 1].SetActive(false);
        ob[level].SetActive(true);
    }

    private IEnumerator Efect()
    {
        GameObject efect = Instantiate(efectprefab, efectpsition.transform.position + new Vector3(0, 0, 70), Quaternion.Euler(-90, 0, 0));
        efect.SetActive(true);

        yield return new WaitForSeconds(0.2f);
        efect.SetActive(false);
        Destroy(efect);
    }

    public int currentLv() { return level; }

    public int currentCost() { return cost[level]; }

}
