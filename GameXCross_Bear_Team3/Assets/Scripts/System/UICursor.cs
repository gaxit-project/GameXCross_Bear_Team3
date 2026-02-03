using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UICursor : MonoBehaviour
{
    [SerializeField] float scrollSpeed = 10f; // �X�N���[���̑���
    [SerializeField] public GameObject cursor;

    [Header("SE�ݒ�")]
    [SerializeField] private string moveSE = "Select"; // �炵����SE�̃L�[��
    private GameObject lastSelected; // �u�������܂őI������Ă������́v���o����ϐ�


    void Start()
    {
        // �ŏ��͌��݂̑I����Ԃ������l�Ƃ��ē���Ă����i�J���ŉ�����̂�h�����߁j
        lastSelected = EventSystem.current.currentSelectedGameObject;
    }


    void Update()
    {
        // ���I������Ă���I�u�W�F�N�g���擾
        GameObject selected = EventSystem.current.currentSelectedGameObject;

        // �����I������Ă��Ȃ��A�܂��͑I�����ꂽ���̂�ScrollRect�̒��g�łȂ��ꍇ�͉������Ȃ�
        if (selected == null )
        {
            return;
        }

        if (Time.timeScale <= 0)
    {
        lastSelected = selected; // 選択状態の同期だけ行っておく
        return;
    }

        if (selected != lastSelected)
        {
            // �I������Ă�����̂��u�������v�ƈႤ�Ȃ�A�ړ������Ƃ������Ɓ�����炷
            SEmanager.Instance.Play("cursor");

            // �u�������v���u���v�̏��ŏ㏑���X�V
            lastSelected = selected;
        }

        // �I�����ꂽ�{�^����Transform���擾
        Transform target = selected.GetComponent<Transform>();

        float targetX = target.position.x;

        // ���݂̈ʒu�ƖڕW�ʒu�̊Ԃ��X���[�Y�Ɉړ��iLerp�j
        Vector3 newPos = cursor.transform.position;
        newPos.x = Mathf.Lerp(newPos.x, targetX, Time.deltaTime * scrollSpeed);
        cursor.transform.position = newPos;
    }
}
