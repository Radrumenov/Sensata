// MOCK ДАННИ ЗА ТРИТЕ СЦЕНАРИЯ
const mockData = {
  1: {
    lineId: "L1",
    healthStatus: "⚠ Предупреждение",
    scenario: "Микро-спирания",
    shift: "Смяна 2",
    date: "17.04.2026",
    kpis: {
      availability: 98, performance: 82,
      quality: 97, oee: 78, energyIntensity: 1.24
    },
    targets: {
      availability: 95, performance: 95,
      quality: 98, oee: 85, energyIntensity: 1.10
    },
    anomalies: [
      "⚠ Performance под прага — 82% при цел 95%",
      "⚠ Енергоинтензивност над baseline",
      "⚠ Несъответствие: висока Наличност при ниска Performance"
    ],
    trend: [
      { shift: "Смяна 1", oee: 84 },
      { shift: "Смяна 2", oee: 81 },
      { shift: "Смяна 3", oee: 79 },
      { shift: "Смяна 4", oee: 78 },
      { shift: "Смяна 5", oee: 78 }
    ],
    operatorComment: {
      date: "17.04.2026", shift: "Смяна 2",
      comment: "Чести задръствания на конвейера, изискващи ръчна намеса."
    },
    historicalMatches: [
      "Случай 14.10.2025 — Сходен спад на Performance при конвейерно разместване"
    ],
    copilotSummary: {
      currentState: "Линия L1 работи с висока наличност, но значително намалена ефективност, което индикира повтарящи се прекъсвания без пълно спиране.",
      keyAnomalies: [
        "Performance: 82% при цел 95% (-13%)",
        "OEE: 78% при цел 85%",
        "Енергоинтензивност над базовата линия"
      ],
      probableCauses: [
        "Вероятно повтарящи се микро-спирания, незасичани от стандартните аларми",
        "Операторският коментар за задръствания подкрепя тази хипотеза",
        "Исторически случай от 14.10.2025 показва сходен модел"
      ],
      recommendedActions: [
        "Проверка на конвейерното наклонение и синхронизация",
        "Преглед на лога за микро-спирания в последните 3 смени",
        "Инспекция на консистентността на суровините"
      ]
    }
  },
  2: {
    lineId: "L1",
    healthStatus: "🔴 Критично",
    scenario: "Performance vs Качество",
    shift: "Смяна 3",
    date: "17.04.2026",
    kpis: {
      availability: 96, performance: 104,
      quality: 91, oee: 81, energyIntensity: 1.15
    },
    targets: {
      availability: 95, performance: 95,
      quality: 98, oee: 85, energyIntensity: 1.10
    },
    anomalies: [
      "🔴 Качество под прага — 91% при цел 98%",
      "⚠ Performance над номинала — линията е ускорена",
      "⚠ Конфликт между Performance и Качество"
    ],
    trend: [
      { shift: "Смяна 1", oee: 85 },
      { shift: "Смяна 2", oee: 84 },
      { shift: "Смяна 3", oee: 83 },
      { shift: "Смяна 4", oee: 81 },
      { shift: "Смяна 5", oee: 79 }
    ],
    operatorComment: {
      date: "17.04.2026", shift: "Смяна 3",
      comment: "Линията е ускорена за навакасване на изоставане от предходната смяна."
    },
    historicalMatches: [
      "Случай 02.03.2025 — Ускорение на линията доведе до 8% брак при подобни условия"
    ],
    copilotSummary: {
      currentState: "Линията работи над номиналната скорост, което е причинило значителен спад в качеството на продукцията.",
      keyAnomalies: [
        "Качество: 91% при цел 98% (-7%)",
        "Performance: 104% — над номинала",
        "OEE: 81% при цел 85%"
      ],
      probableCauses: [
        "Ускорението на линията вероятно е причина за повишен брак",
        "При по-висока скорост параметрите на свързване излизат извън толеранс",
        "Исторически случай от 02.03.2025 потвърждава тази зависимост"
      ],
      recommendedActions: [
        "Връщане към номиналната скорост незабавно",
        "Преглед на бракуваните единици от последните 2 смени",
        "Проверка на параметрите на свързване при текущата скорост"
      ]
    }
  },
  3: {
    lineId: "L1",
    healthStatus: "⚠ Предупреждение",
    scenario: "Предиктивна Поддръжка",
    shift: "Смяна 1",
    date: "17.04.2026",
    kpis: {
      availability: 97, performance: 93,
      quality: 98, oee: 84, energyIntensity: 1.41
    },
    targets: {
      availability: 95, performance: 95,
      quality: 98, oee: 85, energyIntensity: 1.10
    },
    anomalies: [
      "⚠ Енергоинтензивност расте плавно 5 дни",
      "⚠ Отклонение от baseline: +28%",
      "⚠ Исторически съвпадение с повреда на лагер"
    ],
    trend: [
      { shift: "Ден 1", oee: 86 },
      { shift: "Ден 2", oee: 85 },
      { shift: "Ден 3", oee: 85 },
      { shift: "Ден 4", oee: 84 },
      { shift: "Ден 5", oee: 84 }
    ],
    operatorComment: {
      date: "17.04.2026", shift: "Смяна 1",
      comment: "Лека вибрация забелязана при главния задвижващ механизъм."
    },
    historicalMatches: [
      "Случай 08.07.2024 — Постепенно нарастване на енергоинтензивност предшества повреда на лагер след 6 дни"
    ],
    copilotSummary: {
      currentState: "OEE остава близо до целта, но устойчивото нарастване на енергоинтензивността индикира ранен предупредителен сигнал за механичен проблем.",
      keyAnomalies: [
        "Енергоинтензивност: 1.41 kWh/ед при цел 1.10 (+28%)",
        "Тренд: 5 последователни дни на нарастване",
        "Исторически съвпадение с повреда на лагер"
      ],
      probableCauses: [
        "Вероятно механично триене или повишено съпротивление в задвижването",
        "Възможна деградация на лагер или охладителна система",
        "Операторската забележка за вибрация подкрепя тази хипотеза"
      ],
      recommendedActions: [
        "Планиране на превантивен преглед в следващия maintenance прозорец",
        "Проверка на вибрациите с диагностичен инструмент",
        "Мониторинг на енергоинтензивността на всеки 4 часа"
      ]
    }
  }
};

// ГЛОБАЛНА РЕФЕРЕНЦИЯ КЪМ ГРАФИКАТА
let trendChart = null;

// ЗАРЕЖДАНЕ НА СЦЕНАРИЙ
async function loadDashboard(scenarioId) {
  const data = mockData[scenarioId];

  // Мета бар
  document.getElementById("meta-line").textContent = data.lineId;
  document.getElementById("meta-shift").textContent = data.shift;
  document.getElementById("meta-date").textContent = data.date;
  document.getElementById("meta-scenario").textContent = data.scenario;

  // Health badge
  const badge = document.getElementById("health-badge");
  badge.textContent = data.healthStatus;
  badge.className = data.healthStatus.includes("Критично") ? "badge-red" : "badge-yellow";

  // KPI карти
  updateKpiCard("availability", data.kpis.availability, data.targets.availability, "%");
  updateKpiCard("performance", data.kpis.performance, data.targets.performance, "%");
  updateKpiCard("quality", data.kpis.quality, data.targets.quality, "%");
  updateKpiCard("oee", data.kpis.oee, data.targets.oee, "%");
  updateKpiCard("energy", data.kpis.energyIntensity, data.targets.energyIntensity, " kWh/ед");

  // Аномалии
  document.getElementById("anomaly-list").innerHTML =
    data.anomalies.map(a => `<li>${a}</li>`).join("");

  // Исторически съвпадения
  document.getElementById("historical-list").innerHTML =
    data.historicalMatches.map(h => `<li>📋 ${h}</li>`).join("");

  // Операторски коментар
  document.getElementById("comment-date").textContent =
    `${data.operatorComment.date} — ${data.operatorComment.shift}`;
  document.getElementById("comment-text").textContent =
    data.operatorComment.comment;

  // Графика
  renderChart(data.trend);

  // Нулира Copilot панела
  resetCopilot();
}

// KPI КАРТА
function updateKpiCard(id, actual, target, unit) {
  const card = document.getElementById(`kpi-${id}`);
  const delta = actual - target;
  const status = delta >= 0 ? "green" : delta > -5 ? "yellow" : "red";

  card.querySelector(".kpi-value").textContent = `${actual}${unit}`;
  card.querySelector(".kpi-target").textContent = `Цел: ${target}${unit}`;
  card.querySelector(".kpi-delta").textContent =
    `${delta > 0 ? "+" : ""}${delta.toFixed(1)}${unit}`;
  card.className = `kpi-card status-${status}`;
}

// ГРАФИКА
function renderChart(trend) {
  const ctx = document.getElementById("trend-chart").getContext("2d");
  if (trendChart) trendChart.destroy();

  trendChart = new Chart(ctx, {
    type: "line",
    data: {
      labels: trend.map(t => t.shift),
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
        y: {
          ticks: { color: "#aaa" }, grid: { color: "#333" },
          min: 70, max: 100
        }
      }
    }
  });
}

// COPILOT БУТОН
document.getElementById("copilot-btn").addEventListener("click", () => {
  const activeBtn = document.querySelector(".scenario-btn.active");
  const scenarioId = activeBtn ? activeBtn.dataset.scenario : 1;
  const summary = mockData[scenarioId].copilotSummary;

  document.getElementById("copilot-state").textContent = summary.currentState;
  document.getElementById("copilot-anomalies").innerHTML =
    summary.keyAnomalies.map(a => `<li>${a}</li>`).join("");
  document.getElementById("copilot-causes").innerHTML =
    summary.probableCauses.map(c => `<li>${c}</li>`).join("");
  document.getElementById("copilot-actions").innerHTML =
    summary.recommendedActions.map(a => `<li>${a}</li>`).join("");
});

// НУЛИРА COPILOT ПАНЕЛА ПРИ СМЯНА НА СЦЕНАРИЙ
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
