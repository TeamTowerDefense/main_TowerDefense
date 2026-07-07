using UnityEngine;
using System.IO;

public class TextureGenerator : MonoBehaviour
{
    [ContextMenu("Generate Gradient Texture")]
    public void Generate()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 중심점 계산
                float dx = (x - size / 2f) / (size / 2f);
                float dy = (y - size / 2f) / (size / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 거리가 멀어질수록 투명해지는 그라데이션 (0~1)
                float alpha = Mathf.Clamp01(1f - dist);

                // 부드러운 그라데이션을 위해 제곱 적용
                alpha = alpha * alpha;

                colors[y * size + x] = new Color(1, 1, 1, alpha);
            }
        }

        tex.SetPixels(colors);
        tex.Apply();

        // 생성된 이미지를 프로젝트 창에 저장
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/RadialGradient.png", bytes);

        Debug.Log("생성 완료! 프로젝트 창을 확인하세요.");
    }
}