﻿using TriInspectorMVCE;
using UnityEngine;

namespace MetaverseCloudEngine.Unity.Components
{
    [HideMonoScript]
    [DisallowMultipleComponent]
    public class OrthographicZoomPanCamera : TriInspectorMonoBehaviour
    {
        [Tooltip("The camera that will be used for zooming and panning.")]
        [Required, SerializeField] private Camera m_Camera;
        [Tooltip("The speed at which the camera will zoom in and out.")]
        [Min(0)]
        [SerializeField] private float m_ZoomSpeed = 1f;
        [Min(0)]
        [SerializeField] private float m_MinZoom = 0.1f;
        [Min(0)]
        [SerializeField] private float m_MaxZoom = 100f;
        [Tooltip("The speed at which the camera will pan.")]
        [Range(0, 5)]
        [SerializeField] private float m_PanSpeed = 1f;
        [Range(0, 2)]
        [SerializeField] private int m_PanMouseButton = 1;
        
        private float m_LastPinchDistance;
        private Vector2 m_LastTouchPosition;
        private bool m_Initiated;
        
        /// <summary>
        /// The camera that will be used for zooming and panning.
        /// </summary>
        public Camera Camera
        {
            get => m_Camera;
            set => m_Camera = value;
        }

        private void Start()
        {
            if (m_Camera) return;
            MetaverseProgram.Logger.LogError("Camera is not set.");
            enabled = false;
        }

        private void Update()
        {
            if (!m_Camera)
            {
                MetaverseProgram.Logger.LogError("Camera is not set.");
                enabled = false;
                return;
            }

            if (!m_Camera.orthographic)
            {
                m_Initiated = false;
                return;
            }

            if (!UnityEngine.Device.Application.isMobilePlatform)
            {
                var isOverUI = MVUtils.IsPointerOverUI();
                if (Input.GetMouseButtonDown(m_PanMouseButton) && !isOverUI)
                    m_Initiated = true;
                if (Input.GetMouseButton(m_PanMouseButton) && m_Initiated)
                {
                    var pan = new Vector3(
                        Input.GetAxis("Mouse X"), 
                        Input.GetAxis("Mouse Y"), 0) * (m_Camera.orthographicSize * m_PanSpeed);
                    pan = m_Camera.transform.rotation * pan * Time.deltaTime;
                    m_Camera.transform.position -= pan;
                }
                if (Input.GetMouseButtonUp(m_PanMouseButton))
                    m_Initiated = false;

                if (Input.mouseScrollDelta.y != 0 && (!isOverUI || m_Initiated))
                    m_Camera.orthographicSize = Mathf.Clamp(m_Camera.orthographicSize - Input.mouseScrollDelta.y * m_ZoomSpeed, m_MinZoom, m_MaxZoom);
                return;
            }
            
            switch (Input.touchCount)
            {
                case 1:
                {
                    var touch = Input.GetTouch(0);
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            m_LastTouchPosition = touch.position;
                            m_Initiated = !MVUtils.IsPointerOverUI();
                            break;
                        case TouchPhase.Moved when m_Initiated:
                            var pan = new Vector3(
                                touch.position.x - m_LastTouchPosition.x,
                                touch.position.y - m_LastTouchPosition.y, 0) * (m_Camera.orthographicSize * m_PanSpeed);
                            pan = m_Camera.transform.rotation * pan * Time.deltaTime;
                            m_Camera.transform.position -= pan;
                            break;
                        case TouchPhase.Ended:
                            m_Initiated = false;
                            break;
                    }

                    m_LastTouchPosition = touch.position;
                    break;
                }
                case 2:
                {
                    var touch0 = Input.GetTouch(0);
                    var touch1 = Input.GetTouch(1);
                    var pinchDistance = Vector2.Distance(touch0.position, touch1.position);
                
                    if ((touch1.phase == TouchPhase.Began ||
                         touch0.phase == TouchPhase.Began) && !m_Initiated)
                    {
                        m_LastPinchDistance = pinchDistance;
                        m_Initiated = !MVUtils.IsPointerOverUI();
                    }
                
                    if (touch0.phase == TouchPhase.Ended ||
                        touch1.phase == TouchPhase.Ended)
                    {
                        m_Initiated = false;
                        return;
                    }
                
                    if (m_LastPinchDistance != 0 && (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved) && m_Initiated)
                        m_Camera.orthographicSize = Mathf.Clamp(m_Camera.orthographicSize - (pinchDistance - m_LastPinchDistance) * m_ZoomSpeed, m_MinZoom, m_MaxZoom);
                
                    m_LastPinchDistance = pinchDistance;
                    break;
                }
                default:
                    m_Initiated = false;
                    break;
            }
        }
    }
}