using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class ArrayTest : MonoBehaviour {

	// Use this for initialization
	void Start () {
        List<string> hi = new List<string>();
        List<string> hello = new List<string>();
        hi.Add("blah");
        hello.Add("blah");
        List<string>[] hey = new List<string>[] { hi, hello };
        var t = Array.Find(hey, x => x[0] == "b");
        if (t == null) Debug.LogFormat("success");
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
