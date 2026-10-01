// mermaid 순서도를 렌더링한 뒤 A4 한 페이지 안(가로 180mm, 세로 maxHeightMm)에 들어오도록 크기를 맞춘다.
// 큰 순서도가 페이지 밖으로 넘쳐 빈 페이지가 생기는 문제를 막는다. battle-flow.html·mvp-report.html 공용.
(function () {
  const MM = 96 / 25.4;
  const maxWidth = 180 * MM;
  mermaid.initialize({ startOnLoad: false, theme: "neutral", fontFamily: "Apple SD Gothic Neo, sans-serif",
    flowchart: { htmlLabels: true, curve: "basis", nodeSpacing: 26, rankSpacing: 30 } });
  window.addEventListener("load", async () => {
    await mermaid.run({ querySelector: ".mermaid" });
    document.querySelectorAll(".mermaid").forEach((box) => {
      const svg = box.querySelector("svg");
      if (!svg) return;
      const vb = svg.viewBox.baseVal;
      const maxHeight = (parseFloat(box.dataset.maxHeight || "200")) * MM;
      const scale = Math.min(maxWidth / vb.width, maxHeight / vb.height, 1.4);
      svg.removeAttribute("style");
      svg.setAttribute("width", Math.round(vb.width * scale));
      svg.setAttribute("height", Math.round(vb.height * scale));
    });
    document.body.dataset.ready = "1";
  });
})();
