﻿
/*
 * Custom template by Gabriele P.
 */
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using PrattiToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public enum ControllerType { ArmSwing, FootSwing, CVirtualizer };


public class LocomotionManager : UnitySingleton<LocomotionManager>
{
    protected LocomotionManager() { }

    #region Events

    #endregion

    #region Editor Visible

    [SerializeField] private bool _getLocomotionFromConfigFile = true;
    [SerializeField] private ControllerType _locomotion;
    [SerializeField] private List<Transform> _playerControllers;

    [SerializeField] private KeyCode _freezePalyerKeyCode = KeyCode.F;
    //[Expandable]
    [SerializeField] private LocomotionCalibrationData _calibrationData;

    #endregion

    #region Private Members and Constants

    private Vector3 _lastPlayerPosition;
    private float _startingKATmultiply, _startingKATmultiplyback;
    private bool _isPlayerFreezed;

    #endregion

    #region Properties

    public float CurrentPlayerSpeed { get; private set; }
    public Transform CurrentPlayerController { get; private set; }
    public Transform LeftController { get; private set; }
    public Transform RightController { get; private set; }
    public Transform LeftTracker { get; private set; }
    public Transform RightTracker { get; private set; }
    public Transform DirectionalTracker { get; private set; }
    public Transform CameraEye { get; private set; }
    public I_UI_HUDController CurrentUIController { get; set; }

    public ControllerType Locomotion
    {
        get
        {
            return _locomotion;
        }

        private set
        {
            _locomotion = value;
        }
    }

    public bool IsPlayerFreezed
    {
        get { return _isPlayerFreezed; }
        set
        {
            _isPlayerFreezed = value;
            if (_isPlayerFreezed)
            {
                StopLocomotion();
                if (CurrentUIController != null) CurrentUIController.ShowFreezeIcon();
            }
            else
            {
                StartLocomotion();
                if (CurrentUIController != null) CurrentUIController.HideFreezeIcon();
            }
        }
    }

    public LocomotionCalibrationData CalibrationData
    {
        get { return _calibrationData; }
        protected set { _calibrationData = value; }
    }



    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        if (_getLocomotionFromConfigFile)
            Locomotion = Configuration.GetEnum("LocomotionMethod", Locomotion);
        _calibrationData = GetOrCreateCalibrationData();
        Assert.IsNotNull(_calibrationData);
        IsPlayerFreezed = false;
    }

    void Start()
    {
        CurrentPlayerController = _playerControllers[(int)Locomotion];
        CurrentPlayerController.gameObject.SetActive((true));
        var ic = CurrentPlayerController.GetComponent<VRItemController>();
        LeftController = ic.LeftController;
        RightController = ic.RightController;
        switch (Locomotion)
        {
            case ControllerType.ArmSwing:
                var cr = CurrentPlayerController.Find("[CameraRig]");
                DirectionalTracker = cr.Find("Tracker (hip)");
                var l = CurrentPlayerController.GetComponent<CircularLimitTracking>();
                CameraEye = l.CameraEye;
                break;
            case ControllerType.FootSwing:
                l = CurrentPlayerController.GetComponent<CircularLimitTracking>();
                CameraEye = l.CameraEye;
                cr = CurrentPlayerController.Find("[CameraRig]");
                DirectionalTracker = cr.Find("Tracker (hip)");
                LeftTracker = cr.Find("Tracker (left)");
                RightTracker = cr.Find("Tracker (right)");
                break;
            case ControllerType.CVirtualizer:
                var pcm = CurrentPlayerController.GetComponent<PlayerColliderManager>();
                CameraEye = pcm.Head;
                break;
        }

        UI_HUDController c = null;
        c = FindObjectOfType<UI_HUDController>();
        Assert.IsNotNull(c);
        var cc = c.GetComponent<Canvas>();
        cc.renderMode = RenderMode.ScreenSpaceCamera;
        cc.worldCamera = CameraEye.transform.GetChildRecursive("UI").GetComponent<Camera>();
        CurrentUIController = c;
        _lastPlayerPosition = CurrentPlayerController.position;

#if !DEBUG || !UNITY_EDITOR
        //StartFreezed
        IsPlayerFreezed = true;
#endif
    }

    private void Update()
    {
        if (Input.GetKeyDown(_freezePalyerKeyCode))
            IsPlayerFreezed = !IsPlayerFreezed;


        CurrentPlayerSpeed = Vector3.Distance(_lastPlayerPosition, CurrentPlayerController.position) / Time.deltaTime;
        _lastPlayerPosition = CurrentPlayerController.position;
    }

    private void OnApplicationQuit()
    {
        CalibrationData.SavePersistent();
    }




    #endregion

    #region Public Methods

    public void StartLocomotion()
    {
        if (CurrentPlayerController == null) return;
        switch (Locomotion)
        {
            case ControllerType.ArmSwing:
                CurrentPlayerController.GetComponentInChildren<FootSwinger>().FootSwingNavigation = true;
                break;
            case ControllerType.FootSwing:
                CurrentPlayerController.GetComponentInChildren<FootSwinger>().FootSwingNavigation = true;
                break;
            case ControllerType.CVirtualizer:
                //unblock CVirt movements here
                break;
        }
    }
    public void StopLocomotion()
    {
        if (CurrentPlayerController == null) return;
        switch (Locomotion)
        {
            case ControllerType.ArmSwing:
                CurrentPlayerController.GetComponentInChildren<FootSwinger>().FootSwingNavigation = false;
                break;
            case ControllerType.FootSwing:
                CurrentPlayerController.GetComponentInChildren<FootSwinger>().FootSwingNavigation = false;
                break;
            case ControllerType.CVirtualizer:
                //block CVirt movements here
                break;
        }
    }
    #endregion

    #region Helper Methods

    private LocomotionCalibrationData GetOrCreateCalibrationData()
    {
        //      string relPath = "Assets/BuildData/LocomotionCalibrationData.asset";
        string dataPath = PersistentSaveLoad.GetDefaultDataPath("LBF_VR", "calibrationData.pgd");
        var calib = PersistentSaveLoad.Load<LocomotionCalibrationData>(dataPath, PersistentSaveLoad.SerializationType.Json);
        if (calib == null)
            calib = new LocomotionCalibrationData();
        //#if UNITY_EDITOR
        //        calib = AssetDatabase.LoadAssetAtPath(relPath, typeof(LocomotionCalibrationData)) as LocomotionCalibrationData;
        //#endif
        //        if (calib == null)
        //        {
        //            calib = ScriptableObject.CreateInstance<LocomotionCalibrationData>();
        //#if UNITY_EDITOR            
        //            AssetDatabase.CreateAsset(calib, relPath);
        //            AssetDatabase.SaveAssets();
        //#endif

        //        }

        return calib;
    }

    #endregion

    #region Events Callbacks

    #endregion

    #region Coroutines

    #endregion

}
