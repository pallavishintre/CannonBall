using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointForce : MonoBehaviour
{

    private Joint joint;

    // Start is called before the first frame update
    void Start()
    {
        joint = GetComponent<Joint>();
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(joint.currentForce);
    }
}
