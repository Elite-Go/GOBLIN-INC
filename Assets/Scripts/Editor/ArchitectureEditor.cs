using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Architecture))]
public class ArchitectureEditor : Editor
{
    private void OnSceneGUI()
    {
        Architecture arch = (Architecture)target;

        // 월드 좌표계 기준의 도착 위치 계산
        Vector3 currentWorldPos = (Vector2)arch.transform.position + arch.ArrivalPos;

        EditorGUI.BeginChangeCheck();

        // 씬 뷰에 드래그 가능한 '점(Sphere)' 형태의 핸들 생성
        // 마우스로 이 점을 잡고 움직여서 arrivalPos를 조작할 수 있습니다.
        Vector3 newWorldPos = Handles.FreeMoveHandle(
            currentWorldPos, 
            Quaternion.identity, 
            0.3f, // 핸들 크기
            Vector3.one * 0.1f, 
            Handles.SphereHandleCap // 구체 형태의 점 핸들
        );

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(arch, "Move Arrival Position");

            // 이동한 월드 좌표를 다시 상대 좌표(arrivalPos)로 변환
            Vector2 newArrivalPos = (Vector2)newWorldPos - (Vector2)arch.transform.position;
            arch.ArrivalPos = newArrivalPos;

            EditorUtility.SetDirty(arch);
        }

        // 오브젝트 위치와 도착 지점을 연결하는 시각화 선
        Handles.color = Color.green;
        Handles.DrawLine(arch.transform.position, currentWorldPos);

        // 도착 지점에 텍스트 표시 (선택사항)
        Handles.Label(currentWorldPos + Vector3.up * 0.4f, "Arrival Pos");
    }
}