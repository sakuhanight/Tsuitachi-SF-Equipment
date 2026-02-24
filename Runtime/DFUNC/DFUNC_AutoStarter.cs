using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using SaccFlightAndVehicles;
using SFAdvEquipment.SFEXT;
using SFAdvEquipment.Utility;

namespace SFAdvEquipment.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [DefaultExecutionOrder(1000)]
    public class DFUNC_AutoStarter : UdonSharpBehaviour
    {
        public const byte STATE_OFF = 0;
        public const byte STATE_APU_START = 1;
        public const byte STATE_APU_STOP = 2;
        public const byte STATE_ENGINE_START = 3;
        public const byte STATE_ENGINE_STOP = 4;
        public const byte STATE_ON = 255;

        public UdonSharpBehaviour SAVControl;
        public SFEXT_AuxiliaryPowerUnit apu;
        public UdonSharpBehaviour[] engines;

        public KeyCode startKey = KeyCode.LeftShift;
        public KeyCode stopKey = KeyCode.RightControl;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;
        public bool desktopOnly;

        [Header("Engine")]
        [Tooltip("[s]")] public float engineStartInterval = 30.0f;
        [Tooltip("[s]")] public float engineStopInterval = 30.0f;

        [NonSerialized][UdonSynced] public byte state;
        [NonSerialized] public bool start;

        [NonSerialized] public SaccEntity EntityControl;
        [NonSerialized] public bool LeftDial;
        [NonSerialized] public int DialPosition = -999;

        private byte prevState;
        private bool initialized, selected, isPilot, isPassenger, isOwner, prevTrigger;
        private float stateChangedTime;

        public void SFEXT_L_EntityStart()
        {
            start = false;
            state = STATE_OFF;

            SFAEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, false);

            gameObject.SetActive(false);
            initialized = true;
        }

        public void DFUNC_LeftDial() { }
        public void DFUNC_RightDial() { }
        public void DFUNC_Selected() { selected = true; }
        public void DFUNC_Deselected() { selected = false; }

        public void SFEXT_O_PilotEnter()
        {
            if (desktopOnly && Networking.LocalPlayer.IsUserInVR()) return;

            isPilot = true;
            isOwner = true;
            selected = false;
            prevTrigger = true;
            gameObject.SetActive(true);
        }

        public void SFEXT_O_PilotExit() { isPilot = false; }
        public void SFEXT_P_PassengerEnter() { isPassenger = true; }
        public void SFEXT_P_PassengerExit() { isPassenger = false; }

        public void SFEXT_G_Explode() { ResetStatus(); }
        public void SFEXT_G_RespawnButton() { ResetStatus(); }
        public void SFEXT_O_TakeOwnership() { isOwner = true; }
        public void SFEXT_O_LoseOwnership() { isOwner = false; }

        private void ResetStatus()
        {
            state = STATE_OFF;
            start = false;
        }

        private bool holdThrottle;
        private void Update()
        {
            if (!initialized) return;

            var time = Time.time;

            if (isPilot)
            {
                if (Input.GetKeyDown(startKey))
                {
                    if (!start)
                    {
                        SetStart(true);
                        holdThrottle = true;
                    }
                }
                else if (Input.GetKeyDown(stopKey)) SetStart(false);

                if (holdThrottle && Input.GetKeyUp(startKey))
                {
                    holdThrottle = false;
                    SAVControl.SetProgramVariable("ThrottleInput", 0f);
                }

                var trigger = selected && SFAEUtil.IsTriggerPressed(LeftDial);
                if (!prevTrigger && trigger) SetStart(!start);
                prevTrigger = trigger;
            }

            if (isOwner)
            {
                SFAEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, start);
            }
            else if (isPassenger)
            {
                var remoteStart = state != STATE_OFF;
                SFAEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, remoteStart);
            }

            var stateChanged = state != prevState;
            prevState = state;
            if (stateChanged) stateChangedTime = time;
            var stateTime = time - stateChangedTime;

            switch (state)
            {
                case STATE_OFF:
                    if (start) SetState(STATE_APU_START);
                    break;
                case STATE_APU_START:
                    if (isOwner)
                    {
                        if (stateChanged && apu) apu.StartAPU();
                        if (!apu || apu.started) SetState(STATE_ENGINE_START);
                    }
                    break;
                case STATE_APU_STOP:
                    if (isOwner)
                    {
                        if (stateChanged && apu) apu.StopAPU();
                        if (!apu || apu.terminated) SetState(start ? STATE_ON : STATE_OFF);
                    }
                    break;
                case STATE_ENGINE_START:
                    if (isOwner)
                    {
                        var starterIndex = engines.Length - Mathf.FloorToInt(stateTime / engineStartInterval) - 1;
                        if (starterIndex >= 0 && starterIndex < engines.Length)
                            engines[starterIndex].SetProgramVariable("starter", true);
                    }

                    var allEngineStarted = true;
                    foreach (var engine in engines)
                    {
                        if (!engine) continue;
                        var n2 = (float)engine.GetProgramVariable("n2");
                        var minN2 = (float)engine.GetProgramVariable("minN2");
                        var n1 = (float)engine.GetProgramVariable("n1");
                        var idleN1 = (float)engine.GetProgramVariable("idleN1");

                        if (n2 >= minN2)
                        {
                            if (isOwner) engine.SetProgramVariable("fuel", true);
                        }

                        if (n1 >= idleN1 * 0.9f)
                        {
                            if (isOwner) engine.SetProgramVariable("starter", false);
                        }
                        else
                        {
                            allEngineStarted = false;
                        }
                    }

                    if (allEngineStarted) SetState(STATE_APU_STOP);
                    break;
                case STATE_ENGINE_STOP:
                    var index = engines.Length - Mathf.FloorToInt(stateTime / engineStopInterval) - 1;
                    if (index < 0) SetState(STATE_OFF);
                    else if (index < engines.Length && isOwner)
                    {
                        engines[index].SetProgramVariable("starter", false);
                        engines[index].SetProgramVariable("fuel", false);
                    }
                    break;
                case STATE_ON:
                    if (!start) SetState(STATE_ENGINE_STOP);
                    break;
            }

            if (isOwner && !isPilot && (state == STATE_ON || state == STATE_OFF)) gameObject.SetActive(false);
        }

        public override void PostLateUpdate()
        {
            if (isOwner && holdThrottle) SAVControl.SetProgramVariable("ThrottleInput", 0f);
        }

        private void SetStart(bool value)
        {
            start = value;
        }

        private void SetState(byte value)
        {
            if (!isOwner) return;
            state = value;
            RequestSerialization();
        }
    }
}
