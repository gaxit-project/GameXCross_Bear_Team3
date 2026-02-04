using UnityEngine;

public class HunterBullet : MonoBehaviour
{
    private float _speed;
    private Transform _target;

    public void Launch(Transform target, float speed)
    {
        _target = target;
        _speed = speed;

        // 当たらなければ一定時間後に削除
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        if(_target == null)
        {
            transform.Translate(Vector3.forward * _speed * Time.deltaTime);
            return;
        }

        Vector3 direction = (_target.position + Vector3.up * 8.0f - transform.position).normalized;

        transform.position += direction * _speed * Time.deltaTime;
        transform.forward = direction;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Bear") || other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
