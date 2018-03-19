﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class MFNN {

	public bool Inspect = false;

	public string Folder = string.Empty;
	
	public int XDimBlend = 12;
	public int HDimBlend = 12;
	public int YDimBlend = 4;
	public int XDim = 504;
	public int HDim = 512;
	public int YDim = 352;
	public int[] ControlNeurons = new int[0];

	public NetworkParameters Parameters;
	
	/*
	private IntPtr Xmean, Xstd, Ymean, Ystd;
	private IntPtr BW0, BW1, BW2, Bb0, Bb1, Bb2;
	private IntPtr[] M;
	private IntPtr X, Y;

	private List<IntPtr> Ptrs;

    [DllImport("Eigen")]
    private static extern IntPtr Create(int rows, int cols);
    [DllImport("Eigen")]
    private static extern IntPtr Delete(IntPtr m);
    [DllImport("Eigen")]
    private static extern void Add(IntPtr lhs, IntPtr rhs, IntPtr result);
    [DllImport("Eigen")]
    private static extern void Sub(IntPtr lhs, IntPtr rhs, IntPtr result);
    [DllImport("Eigen")]
    private static extern void Multiply(IntPtr lhs, IntPtr rhs, IntPtr result);
    [DllImport("Eigen")]
    private static extern void Scale(IntPtr lhs, float value, IntPtr result);
    [DllImport("Eigen")]
    private static extern void PointwiseMultiply(IntPtr lhs, IntPtr rhs, IntPtr result);
    [DllImport("Eigen")]
    private static extern void PointwiseDivide(IntPtr lhs, IntPtr rhs, IntPtr result);
    [DllImport("Eigen")]
    private static extern void SetValue(IntPtr m, int row, int col, float value);
    [DllImport("Eigen")]
    private static extern float GetValue(IntPtr m, int row, int col);
    [DllImport("Eigen")]
    private static extern void ELU(IntPtr m);
    [DllImport("Eigen")]
    private static extern void SoftMax(IntPtr m);

	public MFNN() {
		Ptrs = new List<IntPtr>();
	}

	~MFNN() {
		for(int i=0; i<Ptrs.Count; i++) {
			Delete(Ptrs[i]);
		}
	}

	public void LoadParameters() {
		Parameters = ScriptableObject.CreateInstance<NetworkParameters>();
		Parameters.StoreMatrix(Folder+"/Xmean.bin", XDim, 1);
		Parameters.StoreMatrix(Folder+"/Xstd.bin", XDim, 1);
		Parameters.StoreMatrix(Folder+"/Ymean.bin", YDim, 1);
		Parameters.StoreMatrix(Folder+"/Ystd.bin", YDim, 1);

		Parameters.StoreMatrix(Folder+"/wc0_w.bin", HDimBlend, XDimBlend);
		Parameters.StoreMatrix(Folder+"/wc0_b.bin", HDimBlend, 1);

		Parameters.StoreMatrix(Folder+"/wc1_w.bin", HDimBlend, HDimBlend);
		Parameters.StoreMatrix(Folder+"/wc1_b.bin", HDimBlend, 1);
		
		Parameters.StoreMatrix(Folder+"/wc2_w.bin", YDimBlend, HDimBlend);
		Parameters.StoreMatrix(Folder+"/wc2_b.bin", YDimBlend, 1);

		for(int i=0; i<YDimBlend; i++) {
			Parameters.StoreMatrix(Folder+"/cp0_a"+i.ToString("D1")+".bin", HDim, XDim);
			Parameters.StoreMatrix(Folder+"/cp0_b"+i.ToString("D1")+".bin", HDim, 1);

			Parameters.StoreMatrix(Folder+"/cp1_a"+i.ToString("D1")+".bin", HDim, HDim);
			Parameters.StoreMatrix(Folder+"/cp1_b"+i.ToString("D1")+".bin", HDim, 1);

			Parameters.StoreMatrix(Folder+"/cp2_a"+i.ToString("D1")+".bin", YDim, HDim);
			Parameters.StoreMatrix(Folder+"/cp2_b"+i.ToString("D1")+".bin", YDim, 1);
		}
	}

	public void Initialise() {
		if(Parameters == null) {
			Debug.Log("Building MFNN failed because no parameters were loaded.");
			return;
		}
		Xmean = Generate(Parameters.GetMatrix(0));
		Xstd = Generate(Parameters.GetMatrix(1));
		Ymean = Generate(Parameters.GetMatrix(2));
		Ystd = Generate(Parameters.GetMatrix(3));

		BW0 = Generate(Parameters.GetMatrix(4));
		Bb0 = Generate(Parameters.GetMatrix(5));
		BW1 = Generate(Parameters.GetMatrix(6));
		Bb1 = Generate(Parameters.GetMatrix(7));
		BW2 = Generate(Parameters.GetMatrix(8));
		Bb2 = Generate(Parameters.GetMatrix(9));

		M = new IntPtr[YDimBlend*6];
		for(int i=0; i<YDimBlend*6; i++) {
			M[i] = Generate(Parameters.GetMatrix(10+i));
		}
		
		X = Create(XDim, 1);
		Ptrs.Add(X);
		Y = Create(YDim, 1);
		Ptrs.Add(Y);
	}

	private IntPtr Generate(NetworkParameters.FloatMatrix matrix) {
		IntPtr ptr = Create(matrix.Rows, matrix.Cols);
		for(int x=0; x<matrix.Rows; x++) {
			for(int y=0; y<matrix.Cols; y++) {
				SetValue(ptr, x, y, matrix.Values[x].Values[y]);
			}
		}
		Ptrs.Add(ptr);
		return ptr;
	}

	public void SetInput(int index, float value) {
		SetValue(X, index, 0, value);
	}

	public float GetOutput(int index) {
		return GetValue(Y, index, 0);
	}

	public float GetControlPoint(int index) {
		return 0f;
	}

	public void Predict() {
		//Setup
		IntPtr CN = Create(ControlNeurons.Length, 1);
		IntPtr CP = Create(YDimBlend, 1);
		IntPtr NNW0 = Create(HDim, XDim);
		IntPtr NNW1 = Create(HDim, HDim);
		IntPtr NNW2 = Create(YDim, HDim);
		IntPtr NNb0 = Create(HDim, 1);
		IntPtr NNb1 = Create(HDim, 1);
		IntPtr NNb2 = Create(YDim, 1);
		IntPtr tmp = Create(1, 1);

        //Normalise input
		Sub(X, Xmean, Y);
		PointwiseDivide(Y, Xstd, Y);
		
        //Process Blending Network
        for(int i=0; i<ControlNeurons.Length; i++) {
            SetValue(CN, i, 0, GetValue(Y, ControlNeurons[i], 0));
        }
        Multiply(BW0, CN, CP); Add(CP, Bb0, CP); ELU(CP);
        Multiply(BW1, CP, CP); Add(CP, Bb1, CP); ELU(CP);
        Multiply(BW2, CP, CP); Add(CP, Bb2, CP); SoftMax(CP);

        //Control Points
		for(int i=0; i<4; i++) {
			Scale(M[6*i + 0], GetValue(CP, i, 0), tmp);
			Add(NNW0, tmp, NNW0);

			Scale(M[6*i + 1], GetValue(CP, i, 0), tmp);
			Add(NNb0, tmp, NNb0);

			Scale(M[6*i + 2], GetValue(CP, i, 0), tmp);
			Add(NNW1, tmp, NNW1);

			Scale(M[6*i + 3], GetValue(CP, i, 0), tmp);
			Add(NNb1, tmp, NNb1);

			Scale(M[6*i + 4], GetValue(CP, i, 0), tmp);
			Add(NNW2, tmp, NNW2);
			
			Scale(M[6*i + 5], GetValue(CP, i, 0), tmp);
			Add(NNb2, tmp, NNb2);
		}

        //Process Mode-Functioned Network
		Multiply(NNW0, Y, Y); Add(Y, NNb0, Y); ELU(Y);
		Multiply(NNW1, Y, Y); Add(Y, NNb1, Y); ELU(Y);
		Multiply(NNW2, Y, Y); Add(Y, NNb2, Y);

        //Renormalise output
		PointwiseMultiply(Y, Ystd, Y);
		Add(Y, Ymean, Y);

		//Cleanup
		Delete(CN);
		Delete(CP);
		Delete(NNW0);
		Delete(NNW1);
		Delete(NNW2);
		Delete(NNb0);
		Delete(NNb1);
		Delete(NNb2);
		Delete(tmp);
	}
	*/

	private IntPtr Network;

    [DllImport("MFNN")]
    private static extern IntPtr Create();
    [DllImport("MFNN")]
    private static extern IntPtr Delete(IntPtr obj);
    [DllImport("MFNN")]
    private static extern void Initialise(IntPtr obj, int xDimBlend, int hDimBlend, int yDimBlend, int xDim, int hDim, int yDim);
    [DllImport("MFNN")]
    private static extern void SetValue(IntPtr obj, int matrix, int row, int col, float value);
    [DllImport("MFNN")]
    private static extern float GetValue(IntPtr obj, int matrix, int row, int col);
    [DllImport("MFNN")]
    private static extern float AddControlNeuron(IntPtr obj, int index);
    [DllImport("MFNN")]
    private static extern void Predict(IntPtr obj);

	public MFNN() {
		Network = Create();
	}

	~MFNN() {
		Delete(Network);
	}

	public void LoadParameters() {
		Parameters = ScriptableObject.CreateInstance<NetworkParameters>();
		Parameters.StoreMatrix(Folder+"/Xmean.bin", XDim, 1);
		Parameters.StoreMatrix(Folder+"/Xstd.bin", XDim, 1);
		Parameters.StoreMatrix(Folder+"/Ymean.bin", YDim, 1);
		Parameters.StoreMatrix(Folder+"/Ystd.bin", YDim, 1);

		Parameters.StoreMatrix(Folder+"/wc0_w.bin", HDimBlend, XDimBlend);
		Parameters.StoreMatrix(Folder+"/wc0_b.bin", HDimBlend, 1);

		Parameters.StoreMatrix(Folder+"/wc1_w.bin", HDimBlend, HDimBlend);
		Parameters.StoreMatrix(Folder+"/wc1_b.bin", HDimBlend, 1);
		
		Parameters.StoreMatrix(Folder+"/wc2_w.bin", YDimBlend, HDimBlend);
		Parameters.StoreMatrix(Folder+"/wc2_b.bin", YDimBlend, 1);

		for(int i=0; i<YDimBlend; i++) {
			Parameters.StoreMatrix(Folder+"/cp0_a"+i.ToString("D1")+".bin", HDim, XDim);
			Parameters.StoreMatrix(Folder+"/cp0_b"+i.ToString("D1")+".bin", HDim, 1);

			Parameters.StoreMatrix(Folder+"/cp1_a"+i.ToString("D1")+".bin", HDim, HDim);
			Parameters.StoreMatrix(Folder+"/cp1_b"+i.ToString("D1")+".bin", HDim, 1);

			Parameters.StoreMatrix(Folder+"/cp2_a"+i.ToString("D1")+".bin", YDim, HDim);
			Parameters.StoreMatrix(Folder+"/cp2_b"+i.ToString("D1")+".bin", YDim, 1);
		}
	}

	public void Initialise() {
		if(Parameters == null) {
			Debug.Log("Building MFNN failed because no parameters were loaded.");
			return;
		}
		Initialise(Network, XDimBlend, HDimBlend, YDimBlend, XDim, HDim, YDim);
		for(int i=0; i<ControlNeurons.Length; i++) {
			AddControlNeuron(ControlNeurons[i]);
		}
		for(int i=0; i<Parameters.Matrices.Length; i++) {
			SetupMatrix(i);
		}
	}

	private void SetupMatrix(int index) {
		NetworkParameters.FloatMatrix matrix = Parameters.GetMatrix(index);
		for(int i=0; i<matrix.Rows; i++) {
			for(int j=0; j<matrix.Cols; j++) {
				SetValue(Network, index, i, j, matrix.Values[i].Values[j]);
			}
		}	
	}

	public void SetInput(int index, float value) {
		if(Parameters == null) {
			return;
		}
		if(index >= XDim) {
			Debug.Log("Setting out of bounds " + index + ".");
			return;
		}
		SetValue(Network, 10+YDimBlend*6, index, 0, value);
	}

	public float GetOutput(int index) {
		if(Parameters == null) {
			return 0f;
		}
		if(index >= YDim) {
			Debug.Log("Returning out of bounds " + index + ".");
			return 0f;
		}
		return GetValue(Network, 10+YDimBlend*6+1, index, 0);
	}

	public void AddControlNeuron(int index) {
		if(Parameters == null) {
			return;
		}
		AddControlNeuron(Network, index);
	}

	public float GetControlPoint(int index) {
		if(Parameters == null) {
			return 0f;
		}
		return GetValue(Network, 10+YDimBlend*6+2, index, 0);
	}

	public void Predict() {
		if(Parameters == null) {
			return;
		}
		Predict(Network);
	}

	#if UNITY_EDITOR
	public void Inspector() {
		Utility.SetGUIColor(Color.grey);
		using(new GUILayout.VerticalScope ("Box")) {
			Utility.ResetGUIColor();
			if(Utility.GUIButton("MFNN", UltiDraw.DarkGrey, UltiDraw.White)) {
				Inspect = !Inspect;
			}

			if(Inspect) {
				using(new EditorGUILayout.VerticalScope ("Box")) {
					Folder = EditorGUILayout.TextField("Folder", Folder);
					XDimBlend = EditorGUILayout.IntField("XDimBlend", XDimBlend);
					HDimBlend = EditorGUILayout.IntField("HDimBlend", HDimBlend);
					YDimBlend = EditorGUILayout.IntField("YDimBlend", YDimBlend);
					XDim = EditorGUILayout.IntField("XDim", XDim);
					HDim = EditorGUILayout.IntField("HDim", HDim);
					YDim = EditorGUILayout.IntField("YDim", YDim);
					Array.Resize(ref ControlNeurons, EditorGUILayout.IntField("Control Neurons", ControlNeurons.Length));
					for(int i=0; i<ControlNeurons.Length; i++) {
						ControlNeurons[i] = EditorGUILayout.IntField("Neuron " + (i+1), ControlNeurons[i]);
					}
					EditorGUILayout.BeginHorizontal();
					if(GUILayout.Button("Load Parameters")) {
						LoadParameters();
					}
					Parameters = (NetworkParameters)EditorGUILayout.ObjectField(Parameters, typeof(NetworkParameters), true);
					EditorGUILayout.EndHorizontal();
				}
			}
		}
	}
	#endif
	
}