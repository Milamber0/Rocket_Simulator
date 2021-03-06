﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class DrawPrediction : MonoBehaviour {

    public Transform apogeeMarker;
    public Transform perigeeMarker;

    public SimController    m_SimController;

    Vector3 apogee = new Vector3();
    Vector3 perigee = new Vector3();
    private float apogeeAltitude = 0.0f;
    private float perigeeAltitude = float.MaxValue;
    //Vector3 previousDebugVector = new Vector3(0, 0, 0);
    private static float startYRocket = 9009f;


    public static double m_OrbitTargetRadius = 6356766f;
    private double m_UnityScaleFix = (m_OrbitTargetRadius / startYRocket);

    //Max vertex detail of the prediction line.
    const int MAX_PREDICTEDPOINTS_ARRAY = 500;


    void Update()
    {
        List<Vector3> predictedPoints = m_SimController.GetPredictedPoints();
        Vector3[] predictedPointsArray = new Vector3[MAX_PREDICTEDPOINTS_ARRAY];
        int predictedPointsIterator = 0;

        apogeeAltitude = 0.0f;
        perigeeAltitude = float.MaxValue;

        if (predictedPoints != null)
        {
	        for (int i = 0; i < predictedPoints.Count; i++)
	        {
	            Vector3 position = predictedPoints[i];
	
	            if (position.magnitude > apogeeAltitude)
	            {
	                apogee = position;
	                apogeeAltitude = position.magnitude;
	            }
	
	            if (position.magnitude < perigeeAltitude)
	            {
	                perigee = position;
	                perigeeAltitude = position.magnitude;
	            }

                //Debug lines gave a more desired result but doesn't show up in the compiled version.
                //if (i % 50 == 0)
                //{
                //       if (i != 0)
                //       {
                //           Debug.DrawLine(previousDebugVector, predictedPoints[i], Color.green);
                //    }
                //    previousDebugVector = predictedPoints[i];
                //}

                //How low i % num is the frequency a point is sampled into the array that's drawn.
                if (i % 250 == 0)
                {
                    if (predictedPointsIterator < MAX_PREDICTEDPOINTS_ARRAY)
                    {
                        predictedPointsArray[predictedPointsIterator++] = predictedPoints[i];
                    }
                }
            }

            //Draw line.
            GetComponent<LineRenderer>().numPositions = predictedPointsIterator;
            GetComponent<LineRenderer>().SetPositions(predictedPointsArray);


            apogeeMarker.position = apogee;
	        perigeeMarker.position = perigee;
        }
    }

    public float GetApogee()
    {
        float apogee = apogeeAltitude * (float)m_UnityScaleFix - (float)m_OrbitTargetRadius;
        if (apogee > 0.0f)
            return apogee;
        else
            return 0.0f;
    }

    public float GetPerigee()
    {
        float perigee = perigeeAltitude * (float)m_UnityScaleFix - (float)m_OrbitTargetRadius;
        if (perigee > 0.0f && !float.IsInfinity(perigee))
            return perigee;
        else
            return 0.0f;
    }
}
