using UnityEngine;

/*
#if UNITY_EDITOR
[UnityEditor.CanEditMultipleObjects]
[UnityEditor.CustomEditor(typeof(P3D_ClickToPaintSubstep))]
public class P3D_ClickToPaintSubstep_Editor : P3D_Editor<P3D_ClickToPaintSubstep>
{
	protected override void OnInspector()
	{
		DrawDefault("Requires");

		DrawDefault("RaycastMask");

		DrawDefault("GroupMask");

		DrawDefault("StepSize");

		DrawDefault("Paint");

		DrawDefault("Brush");
	}
}
#endif
 */

// This script allows you to paint the scene using raycasts
// NOTE: This requires the paint targets have the P3D_Paintable component
public class P3D_ClickToPaintSubstep : MonoBehaviour
{
    public enum NearestOrAll
	{
		Nearest,
		All
	}

	[Tooltip("The key that must be held down to mouse look")]
	public KeyCode Requires = KeyCode.Mouse0;

	[Tooltip("The GameObject layers you want to be able to paint")]
	public LayerMask LayerMask = -1;

	[Tooltip("The paintable texture groups you want to be able to paint")]
	public P3D_GroupMask GroupMask = -1;

	[Tooltip("The maximum amount of pixels between ")]
	public float StepSize = 1.0f;

	[Tooltip("Which surfaces it should hit")]
	public NearestOrAll Paint;

	[Tooltip("The settings for the brush we will paint with")]
	public P3D_Brush Brush;

	private Camera mainCamera;

	private Vector2 oldMousePosition;

    //������ˢsize��
    public KeyCode RequiresForSize1 = KeyCode.LeftControl, RequiresForSize2 = KeyCode.RightControl;

    //������ˢ͸������
    public KeyCode RequiresForOpacity1 = KeyCode.LeftAlt, RequiresForOpacity2 = KeyCode.RightAlt;

    //����/�ر���Ƥ��ģʽ��
    public KeyCode RequiresForErase = KeyCode.E;


    // Called every frame
    protected virtual void Update()
	{
		if (mainCamera == null) mainCamera = Camera.main;

		if (mainCamera != null && StepSize > 0.0f)
		{
			// The required key is down?
            //����ʱ������paint���λ��
			if (Input.GetKeyDown(Requires) == true)
			{
				oldMousePosition = Input.mousePosition;
            }

			// The required key is set?
            //����ʱ��Ҫ����
			if (Input.GetKey(Requires) == true)
			{
				// Find the ray for this screen position
				var newMousePosition = (Vector2)Input.mousePosition;
				var stepCount        = Vector2.Distance(oldMousePosition, newMousePosition) / StepSize + 1;

                //�¼ӣ����ı�ˢ��ɫ���ӵ�ɫ��
                Brush.Color = ColorManager.currentColor;

                for (var i = 0; i < stepCount; i++)
				{
					var subMousePosition = Vector2.Lerp(oldMousePosition, newMousePosition, i / stepCount);
					var ray              = mainCamera.ScreenPointToRay(subMousePosition);
					var start            = ray.GetPoint(mainCamera.nearClipPlane);
					var end              = ray.GetPoint(mainCamera.farClipPlane);

					// This will both use Physics.Raycast and search P3D_Paintables
					switch (Paint)
					{
						case NearestOrAll.Nearest:
						{
							P3D_Paintable.ScenePaintBetweenNearest(Brush, start, end, LayerMask, GroupMask);
						}
						break;

						case NearestOrAll.All:
						{
							P3D_Paintable.ScenePaintBetweenAll(Brush, start, end, LayerMask, GroupMask);
						}
						break;
					}
				}


                //���������saveUndoState�Ĵ��룿


				oldMousePosition = newMousePosition;
			}

            //��/����Ƥ��ģʽ
            if (Input.GetKeyDown(RequiresForErase))
            {
                if(Brush.Blend == P3D_BlendMode.AlphaErase)
                {
                    Brush.Blend = P3D_BlendMode.AlphaBlend;
                    Brush.Detail = null;
                }
            }

            //�����ˢ�ߴ硢͸���ȡ��Թ���Ϊ������д����
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                if (Input.GetKey(RequiresForSize1) == true || Input.GetKey(RequiresForSize2) == true)
                {
                    //���
                    if (Brush.Size.x < 50.0f)
                    {
                        Brush.Size += new Vector2(2.0f, 2.0f);
                    }
                }
                else if (Input.GetKey(RequiresForOpacity1) == true || Input.GetKey(RequiresForOpacity2) == true)
                {
                    //��ʵ��
                    if (Brush.Opacity < 1.0f)
                    {
                        Brush.Opacity += 0.1f;
                    }
                }
            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                if (Input.GetKey(RequiresForSize1) == true || Input.GetKey(RequiresForSize2) == true)
                {
                    //��С
                    if (Brush.Size.x > 2.0f)
                    {
                        Brush.Size -= new Vector2(1.0f, 1.0f);
                    }
                }
                else if (Input.GetKey(RequiresForOpacity1) == true || Input.GetKey(RequiresForOpacity2) == true)
                {
                    //��͸��
                    if (Brush.Opacity > 0.1f)
                    {
                        Brush.Opacity -= 0.1f;
                    }
                }
            }
        }
    }
}
