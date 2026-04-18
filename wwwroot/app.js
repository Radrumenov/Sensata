// MOCK COPILOT SUMMARIES — fallback ако AI endpoint се счупи
const mockCopilot = {
  1: {
    currentState: "Линия L1 работи с висока наличност, но значително намалена ефективност, което индикира повтарящи се прекъсвания без пълно спиране.",
    anomalies: ["Performance: 82% при цел 95% (-13%)", "OEE: 78% при цел 85%", "Енергоинтензивност над базовата линия"],
    rootCauses: ["Вероятно повтарящи се микро-спирания, незасичани от стандартните аларми", "Операторският коментар за задръствания подкрепя тази хипотеза"],
    recommendations: ["Проверка на конвейерното наклонение и синхронизация", "Преглед на лога за микро-спирания в последните 3 смени"]
  },
  2: {
    currentState: "Линията работи над номиналната скорост, което е причинило значителен спад в качеството на продукцията.",
    anomalies: ["Качество: 91% при цел 98% (-7%)", "Performance: 104% — над номинала", "OEE: 81% при цел 85%"],
    rootCauses: ["Ускорението на линията вероятно е причина за повишен брак", "При по-висока скорост параметрите излизат извън толеранс"],
    recommendations: ["Връщане към номиналната скорост незабавно", "Преглед на бракуваните единици от последните 2 смени"]
  },
  3: {
    currentState: "OEE остава близо до целта, но устойчивото нарастване на енергоинтензивността индикира ранен предупредителен сигнал.",
    anomalies: ["Енергоинтензивност: над baseline +28%", "Тренд: 5 последователни дни на нарастване", "Исторически съвпадение с повреда на лагер"],
    rootCauses: ["Вероятно механично триене или повишено съпротивление в задвижването", "Операторската забележка за вибрация подкрепя тази хипотеза"],
    recommendations: ["Планиране на превантивен преглед в следващия maintenance прозорец", "Проверка на вибрациите с диагностичен инструмент"]
  }
};

// ГЛОБАЛНИ ПРОМЕНЛИВИ
let trendChart = null;
let currentLineId = 1;
let currentScenarioIndex = 1;

// ЗАРЕЖДА СЦЕНАРИЙ ОТ РЕАЛНИЯ BACKEND
async function loadDashboard(scenarioId) {
  currentScenarioIndex = scenarioId;

  try {
   const response = await fetch('scenarios.json');
    if (!response.ok) throw new Error("Backend грешка");
    const scenarios = await response.json();

    // Намираме сценария по scenarioId
    const scenario = scenarios.find(s => s.scenarioId == scenarioId)
                  || scenarios[scenarioId - 1];
    if (!scenario) throw new Error("Сценарият не е намерен");

    // Запазваме lineId за Copilot повикването
    currentLineId = scenario.lineId;

    // Последната смяна = текущите KPI стойности
    const trend = scenario.trend || [];
    const lastShift = trend[trend.length - 1];
    if (!lastShift) throw new Error("Няма trend данни");

    // Targets от backend или defaults
    const targets = scenario.targets || {
      oee: 85, quality: 98, availability: 95, performance: 95, energyIntensity: 1.10
    };

    // МЕТА БАР
    document.getElementById("meta-line").textContent = scenario.lineName || scenario.lineId || "L1";
    document.getElementById("meta-shift").textContent = lastShift.shiftName || "-";
    document.getElementById("meta-date").textContent = lastShift.date || "-";
    document.getElementById("meta-scenario").textContent = scenario.scenarioLabel || "-";

    // HEALTH BADGE
    const badge = document.getElementById("health-badge");
    const oee = lastShift.oee || 0;
    if (oee < 75) { badge.textContent = "🔴 Критично"; badge.className = "badge-red"; }
    else if (oee < 85) { badge.textContent = "⚠ Предупреждение"; badge.className = "badge-yellow"; }
    else { badge.textContent = "✅ Нормално"; badge.className = "badge-green"; }

    // KPI КАРТИ
    updateKpiCard("availability", lastShift.availability, targets.availability || 95, "%");
    updateKpiCard("performance", lastShift.performance, targets.performance || 95, "%");
    updateKpiCard("quality", lastShift.quality, targets.quality || 98, "%");
    updateKpiCard("oee", lastShift.oee, targets.oee || 85, "%");
    updateKpiCard("energy", lastShift.energyIntensity, targets.energyIntensity || 1.10, " kWh/ед");

    // АНОМАЛИИ
    const anomalies = [];
    if (lastShift.performance < (targets.performance || 95))
      anomalies.push(`⚠ Performance под прага — ${lastShift.performance.toFixed(1)}% при цел ${targets.performance || 95}%`);
    if (lastShift.quality < (targets.quality || 98))
      anomalies.push(`⚠ Качество под прага — ${lastShift.quality.toFixed(1)}% при цел ${targets.quality || 98}%`);
    if (lastShift.oee < (targets.oee || 85))
      anomalies.push(`⚠ OEE под прага — ${lastShift.oee.toFixed(1)}% при цел ${targets.oee || 85}%`);
    if (lastShift.energyIntensity > (targets.energyIntensity || 1.10))
      anomalies.push(`⚠ Енергоинтензивност над baseline — ${lastShift.energyIntensity.toFixed(2)} kWh/ед`);
    if (anomalies.length === 0)
      anomalies.push("✅ Няма засечени аномалии");

    document.getElementById("anomaly-list").innerHTML =
      anomalies.map(a => `<li>${a}</li>`).join("");

    // ИСТОРИЧЕСКИ СЪВПАДЕНИЯ
    document.getElementById("historical-list").innerHTML =
      `<li>📋 ${scenario.summary || "Няма исторически съвпадения."}</li>`;

    // ОПЕРАТОРСКИ КОМЕНТАР
    document.getElementById("comment-date").textContent = lastShift.date || "-";
    document.getElementById("comment-text").textContent =
      lastShift.operatorComment && lastShift.operatorComment !== "OK"
        ? lastShift.operatorComment
        : "Няма специален коментар за тази смяна.";

    // ГРАФИКА
    renderChart(trend);
    resetCopilot();

  } catch (err) {
    console.warn("Backend не е достъпен — зареждам mock данни. Грешка:", err.message);
    loadMock(scenarioId);
  }
}

// MOCK FALLBACK
function loadMock(scenarioId) {
  const mock = {
    1: { label: "Микро-спирания", line: "TPMS Assembly Line", shift: "Смяна 2", date: "17.04.2026",
         availability: 98, performance: 82, quality: 97, oee: 78, energy: 1.24,
         comment: "Чести задръствания на конвейера, изискващи ръчна намеса.",
         historical: "Случай 14.10.2025 — Сходен спад при конвейерно разместване",
         trend: [{s:"Смяна 1",oee:84},{s:"Смяна 2",oee:81},{s:"Смяна 3",oee:79},{s:"Смяна 4",oee:78},{s:"Смяна 5",oee:78}]
    },
    2: { label: "Performance vs Качество", line: "Brake Switch Line", shift: "Смяна 3", date: "17.04.2026",
         availability: 96, performance: 104, quality: 91, oee: 81, energy: 1.15,
         comment: "Линията е ускорена за навакасване на изоставане.",
         historical: "Случай 02.03.2025 — Ускорение доведе до 8% брак",
         trend: [{s:"Смяна 1",oee:85},{s:"Смяна 2",oee:84},{s:"Смяна 3",oee:83},{s:"Смяна 4",oee:81},{s:"Смяна 5",oee:79}]
    },
    3: { label: "Предиктивна Поддръжка", line: "TPMS Assembly Line", shift: "Смяна 1", date: "17.04.2026",
         availability: 97, performance: 93, quality: 98, oee: 84, energy: 1.41,
         comment: "Лека вибрация забелязана при главния задвижващ механизъм.",
         historical: "Случай 08.07.2024 — Нарастване на енергоинтензивност преди повреда на лагер",
         trend: [{s:"Ден 1",oee:86},{s:"Ден 2",oee:85},{s:"Ден 3",oee:85},{s:"Ден 4",oee:84},{s:"Ден 5",oee:84}]
    }
  };

  const d = mock[scenarioId];
  document.getElementById("meta-line").textContent = d.line;
  document.getElementById("meta-shift").textContent = d.shift;
  document.getElementById("meta-date").textContent = d.date;
  document.getElementById("meta-scenario").textContent = d.label;

  const badge = document.getElementById("health-badge");
  if (d.oee < 75) { badge.textContent = "🔴 Критично"; badge.className = "badge-red"; }
  else if (d.oee < 85) { badge.textContent = "⚠ Предупреждение"; badge.className = "badge-yellow"; }
  else { badge.textContent = "✅ Нормално"; badge.className = "badge-green"; }

  updateKpiCard("availability", d.availability, 95, "%");
  updateKpiCard("performance", d.performance, 95, "%");
  updateKpiCard("quality", d.quality, 98, "%");
  updateKpiCard("oee", d.oee, 85, "%");
  updateKpiCard("energy", d.energy, 1.10, " kWh/ед");

  const anomalies = [];
  if (d.performance < 95) anomalies.push(`⚠ Performance под прага — ${d.performance}% при цел 95%`);
  if (d.quality < 98) anomalies.push(`⚠ Качество под прага — ${d.quality}% при цел 98%`);
  if (d.oee < 85) anomalies.push(`⚠ OEE под прага — ${d.oee}% при цел 85%`);
  if (d.energy > 1.10) anomalies.push(`⚠ Енергоинтензивност над baseline — ${d.energy} kWh/ед`);

  document.getElementById("anomaly-list").innerHTML = anomalies.map(a => `<li>${a}</li>`).join("");
  document.getElementById("historical-list").innerHTML = `<li>📋 ${d.historical}</li>`;
  document.getElementById("comment-date").textContent = d.date;
  document.getElementById("comment-text").textContent = d.comment;
  renderChart(d.trend.map(t => ({ shiftName: t.s, oee: t.oee })));
  resetCopilot();
}

// KPI КАРТА
function updateKpiCard(id, actual, target, unit) {
  const card = document.getElementById(`kpi-${id}`);
  if (!card || actual === undefined || actual === null) return;
  const delta = actual - target;
  const status = delta >= 0 ? "green" : delta > -5 ? "yellow" : "red";
  card.querySelector(".kpi-value").textContent = `${actual}${unit}`;
  card.querySelector(".kpi-target").textContent = `Цел: ${target}${unit}`;
  card.querySelector(".kpi-delta").textContent = `${delta > 0 ? "+" : ""}${delta.toFixed(1)}${unit}`;
  card.className = `kpi-card status-${status}`;
}

// ГРАФИКА
function renderChart(trend) {
  const ctx = document.getElementById("trend-chart").getContext("2d");
  if (trendChart) trendChart.destroy();
  trendChart = new Chart(ctx, {
    type: "line",
    data: {
      labels: trend.map(t => t.shiftName || t.shift || t.s),
      datasets: [{
        label: "OEE %",
        data: trend.map(t => t.oee),
        borderColor: "#00d4ff",
        backgroundColor: "rgba(0, 212, 255, 0.1)",
        tension: 0.3,
        fill: true
      }]
    },
    options: {
      responsive: true,
      plugins: { legend: { labels: { color: "#e0e0e0" } } },
      scales: {
        x: { ticks: { color: "#aaa" }, grid: { color: "#333" } },
        y: { ticks: { color: "#aaa" }, grid: { color: "#333" }, min: 60, max: 100 }
      }
    }
  });
}

// COPILOT БУТОН — вика /api/data/ai/analyze/{lineId}
document.getElementById("copilot-btn").addEventListener("click", async () => {
  document.getElementById("copilot-state").textContent = "Анализиране...";
  document.getElementById("copilot-anomalies").innerHTML = "";
  document.getElementById("copilot-causes").innerHTML = "";
  document.getElementById("copilot-actions").innerHTML = "";

  try {
    const response = await fetch(`/api/data/ai/analyze/${currentLineId}`);
    if (!response.ok) throw new Error("AI endpoint грешка");
    const result = await response.json();

    // Backend връща: currentState, anomalies, rootCauses, recommendations
    document.getElementById("copilot-state").textContent =
      result.currentState || "-";
    document.getElementById("copilot-anomalies").innerHTML =
      (result.anomalies || []).map(a => `<li>${a}</li>`).join("");
    document.getElementById("copilot-causes").innerHTML =
      (result.rootCauses || []).map(c => `<li>${c}</li>`).join("");
    document.getElementById("copilot-actions").innerHTML =
      (result.recommendations || []).map(a => `<li>${a}</li>`).join("");

  } catch (err) {
    // Fallback към mock
    const summary = mockCopilot[currentScenarioIndex];
    document.getElementById("copilot-state").textContent = summary.currentState;
    document.getElementById("copilot-anomalies").innerHTML =
      summary.anomalies.map(a => `<li>${a}</li>`).join("");
    document.getElementById("copilot-causes").innerHTML =
      summary.rootCauses.map(c => `<li>${c}</li>`).join("");
    document.getElementById("copilot-actions").innerHTML =
      summary.recommendations.map(a => `<li>${a}</li>`).join("");
  }
});

// НУЛИРА COPILOT
function resetCopilot() {
  document.getElementById("copilot-state").textContent = "-";
  document.getElementById("copilot-anomalies").innerHTML = "";
  document.getElementById("copilot-causes").innerHTML = "";
  document.getElementById("copilot-actions").innerHTML = "";
}

// БУТОНИ ЗА СЦЕНАРИИ
document.querySelectorAll(".scenario-btn").forEach(btn => {
  btn.addEventListener("click", () => {
    document.querySelectorAll(".scenario-btn").forEach(b => b.classList.remove("active"));
    btn.classList.add("active");
    loadDashboard(btn.dataset.scenario);
  });
});

// ЗАРЕЖДА СЦЕНАРИЙ 1 ПО ПОДРАЗБИРАНЕ
document.querySelector('[data-scenario="1"]').classList.add("active");
loadDashboard(1);

document.getElementById("workers-ai-btn").addEventListener("click", async () => {
  const btn = document.getElementById("workers-ai-btn");
  btn.textContent = "Zarezhda...";
  try {
    const response = await fetch("/api/data/ai/workers/1");
    if (!response.ok) throw new Error("Greshka");
    const result = await response.json();
    alert(result.situationSummary + " " + result.managerNote);
  } catch (err) {
    alert("Greshka");
  } finally {
    btn.textContent = "AI Preporaka za Zamestnitsi";
  }
});


