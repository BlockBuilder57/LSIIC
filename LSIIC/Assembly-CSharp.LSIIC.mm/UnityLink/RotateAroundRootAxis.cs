using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace LSIIC
{
	public class RotateAroundRootAxis : FVRInteractiveObject
	{
		[Header("Rotate Around Root Axis")]
		public Transform Root;
		public Transform ObjectToRotate;

		public enum RotationAxis { X, Y }
		[Header("Axes and Angles")]
		public RotationAxis Axis = RotationAxis.Y;
		public bool InvertZRoot;
		[Tooltip("x = min, y = max")]
		public Vector2 RotationLimit;

		//[HideInInspector()]
		public Vector3 TargetRotation;
		[Range(0f, 360f)]
		public float AutoComplete = 5f;
		[Tooltip("This might be in rad/s?")]
		public float CompletionRate = 6f;
		private float m_targetAngle;

		[Header("Events")]
		public UnityEvent OnCompleteMin;
		public UnityEvent OnCompleteMax;
		public UnityEvent OnStartedBetween;
		public enum State { Min, Max, Between }
		public State m_curState;
		public State m_prevState;

		public void Update()
		{
			Vector3 cur = ObjectToRotate.localEulerAngles;
			if (cur != TargetRotation)
			{
				ObjectToRotate.localEulerAngles = new Vector3(Mathf.LerpAngle(cur.x, TargetRotation.x, Time.deltaTime * CompletionRate),
															  Mathf.LerpAngle(cur.y, TargetRotation.y, Time.deltaTime * CompletionRate),
															  Mathf.LerpAngle(cur.z, TargetRotation.z, Time.deltaTime * CompletionRate));
			}
		}

		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);

			if (Axis == RotationAxis.X)
			{
				Vector3 vector = hand.transform.position - Root.position;
				vector = Vector3.ProjectOnPlane(vector, Root.right).normalized;
				Vector3 lhs = this.ObjectToRotate.forward;
				if (InvertZRoot)
					lhs = -this.ObjectToRotate.forward;

				float num = Mathf.Atan2(Vector3.Dot(Root.right, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * 57.29578f;
				num = Mathf.Clamp(num, -10f, 10f);
				m_targetAngle += num;
				m_targetAngle = Mathf.Clamp(m_targetAngle, RotationLimit.x, RotationLimit.y);
			}
			else if (Axis == RotationAxis.Y)
			{
				Vector3 vector = hand.transform.position - Root.position;
				vector = Vector3.ProjectOnPlane(vector, Root.up).normalized;
				Vector3 lhs = -Root.transform.forward;
				m_targetAngle = Mathf.Atan2(Vector3.Dot(Root.up, Vector3.Cross(lhs, vector)), Vector3.Dot(lhs, vector)) * Mathf.Rad2Deg;
			}

			if (Mathf.Abs(m_targetAngle - RotationLimit.x) < 3f) //3f is the degree difference needed to set the target angle
				m_targetAngle = RotationLimit.x;

			if (Mathf.Abs(m_targetAngle - RotationLimit.y) < 3f)
				m_targetAngle = RotationLimit.y;

			if (m_targetAngle >= RotationLimit.x && m_targetAngle <= RotationLimit.y)
			{
				if (Axis == RotationAxis.X)
					TargetRotation = new Vector3(m_targetAngle, 0f, 0f);
				else if (Axis == RotationAxis.Y)
					TargetRotation = new Vector3(0f, m_targetAngle, 0f);

				float completion = Mathf.InverseLerp(RotationLimit.x, RotationLimit.y, m_targetAngle);

				if (completion > 1f - AutoComplete / 360f)
					m_curState = State.Max;
				else if (completion < AutoComplete / 360f)
					m_curState = State.Min;
				else
                {
					m_curState = State.Between;
					if (m_prevState != State.Between)
						if (OnStartedBetween != null)
							OnStartedBetween.Invoke();
                }

				if (m_curState == State.Min && m_prevState != State.Min)
					if (OnCompleteMin != null)
						OnCompleteMin.Invoke();
				if (m_curState == State.Max && m_prevState != State.Max)
					if (OnCompleteMax != null)
						OnCompleteMax.Invoke();
				m_prevState = m_curState;
			}
		}
	}
}
