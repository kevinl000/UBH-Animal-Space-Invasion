#!/usr/bin/env python3
"""Builds EnemyAI_Report_<version>.pdf from results.json (written by `./run.sh all`).

Every report is also saved to history/<version>.json, and the ablation chart and the
version table compare all saved versions, so you can see how a change moved the numbers.

Usage:  python3 report.py --version v1.3 [--note "what changed"]
Needs:  matplotlib
"""
import argparse
import glob
import json
import os
import shutil
import subprocess
import textwrap
from datetime import datetime

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.backends.backend_pdf import PdfPages

HERE = os.path.dirname(os.path.abspath(__file__))

# Palette (validated: light surface, slots 1-3 pass CVD / normal-vision checks)
SURFACE = "#fcfcfb"
INK = "#0b0b0b"
INK2 = "#52514e"
MUTED = "#8a8984"
GRID = "#e6e5e1"
SERIES = ["#2a78d6", "#eb6834", "#1baf7a"]  # fixed order: latest version / deer, previous / skunk, ...
NEUTRAL_BAR = "#c9c7c0"
GOOD = "#0ca30c"
CRITICAL = "#d03b3b"

VARIANT_LABEL = {
    "Random": "Random",
    "DesignerOnly": "Designer shares only",
    "RulesOnly": "Rules only",
    "LearningOnly": "Learning only",
    "RulesPlusLearning": "Rules + learning",
    "Oracle": "Oracle",
}

plt.rcParams.update({
    "font.family": "DejaVu Sans",
    "font.size": 9,
    "axes.edgecolor": GRID,
    "axes.labelcolor": INK2,
    "axes.titlecolor": INK,
    "xtick.color": INK2,
    "ytick.color": INK2,
    "axes.spines.top": False,
    "axes.spines.right": False,
    "figure.facecolor": SURFACE,
    "axes.facecolor": SURFACE,
    "savefig.facecolor": SURFACE,
})

PAGE = (8.5, 11)  # US Letter


def git_commit():
    try:
        return subprocess.check_output(["git", "rev-parse", "--short", "HEAD"], cwd=HERE, stderr=subprocess.DEVNULL).decode().strip()
    except Exception:
        return "unknown"


def load_history():
    runs = []
    for p in glob.glob(os.path.join(HERE, "history", "*.json")):
        with open(p) as f:
            d = json.load(f)
        runs.append(d)
    runs.sort(key=lambda d: d["meta"]["saved"])
    return runs


def m(stat):
    return stat["mean"] if stat and stat["mean"] is not None else float("nan")


def ci(stat):
    return stat["ci"] if stat and stat["ci"] is not None else 0.0


def text_block(fig, x, y, lines, size=9, color=INK2, gap=0.018, weight="normal"):
    for i, line in enumerate(lines):
        fig.text(x, y - i * gap, line, fontsize=size, color=color, weight=weight, va="top")
    return y - len(lines) * gap


def kpi(fig, x, y, w, value, label, sub=None):
    fig.text(x, y, value, fontsize=22, color=INK, weight="bold", va="top")
    fig.text(x, y - 0.042, label, fontsize=8.5, color=INK2, va="top", linespacing=1.3)
    if sub:
        fig.text(x, y - 0.080, sub, fontsize=7.5, color=MUTED, va="top")


def page1(pdf, cur, hist):
    fig = plt.figure(figsize=PAGE)
    meta = cur["meta"]
    fig.text(0.07, 0.95, "Enemy AI test report", fontsize=20, weight="bold", color=INK, va="top")
    fig.text(0.07, 0.915, "UB Hacking 2026 · Space tower defense · Wave director with online learning", fontsize=10, color=INK2, va="top")
    fig.text(0.07, 0.892, "Version {}  ·  commit {}  ·  {}  ·  {} simulated levels per test".format(
        meta["version"], meta["commit"], meta["saved"], cur["runs_per_test"]), fontsize=8.5, color=MUTED, va="top")
    if meta.get("note"):
        fig.text(0.07, 0.874, "Change in this version: " + meta["note"], fontsize=8.5, color=INK2, va="top")

    ab = {r["variant"]: r for r in cur["ablation"]}
    rl, rnd = ab["RulesPlusLearning"], ab["Random"]
    ratio = m(rl["deer_share_pct"]) / m(rnd["deer_share_pct"])
    viol = sum(r["budget_violations"] for r in cur["ablation"])
    ec = cur["edge_cases"]
    tiles = [
        ("{:.1f}x".format(ratio), "more of the enemy the player\nis weak to, vs random",
         "{:.0f}% vs {:.0f}% of spent cost".format(m(rl["deer_share_pct"]), m(rnd["deer_share_pct"]))),
        ("{:.2f}x".format(m(rl["pct_of_oracle"]) / m(ab["RulesOnly"]["pct_of_oracle"])), "damage per cost vs the\nrules-only algorithm",
         "see page 2"),
        (str(viol), "times the difficulty\nbudget was exceeded", "across all ablation runs"),
        ("{}/{}".format(ec["passed"], ec["passed"] + ec["failed"]), "edge cases pass", "no exceptions, NaN or hangs"),
    ]
    for i, (v, lab, sub) in enumerate(tiles):
        kpi(fig, 0.07 + i * 0.225, 0.845, 0.2, v, lab, sub)

    # ---- Ablation chart
    ax = fig.add_axes([0.27, 0.40, 0.66, 0.31])
    order = ["Random", "DesignerOnly", "RulesOnly", "LearningOnly", "RulesPlusLearning"]
    versions = hist[-3:]  # up to three versions side by side (fixed slot order: oldest first)

    def ablation_means(run):
        return {r["variant"]: m(r["pct_of_oracle"]) for r in run["ablation"]}
    latest = ablation_means(cur)
    if all(all(abs(ablation_means(v).get(k, 0) - latest[k]) < 0.05 for k in latest) for v in versions):
        versions = [cur]  # AI unchanged across these versions: one set of bars is clearer
    nver = len(versions)
    h = 0.8 / nver
    for vi, run in enumerate(versions):
        rows = {r["variant"]: r for r in run["ablation"]}
        is_latest = run["meta"]["version"] == meta["version"]
        for yi, var in enumerate(order):
            r = rows.get(var)
            if not r:
                continue
            y = yi + (vi - (nver - 1) / 2) * h
            if nver == 1:
                color = SERIES[0] if var == "RulesPlusLearning" else NEUTRAL_BAR
            else:
                color = SERIES[vi]
            ax.barh(y, m(r["pct_of_oracle"]), height=h * 0.86, color=color, xerr=ci(r["pct_of_oracle"]),
                    error_kw=dict(ecolor=INK2, elinewidth=1, capsize=2))
            if is_latest or nver == 1:
                ax.text(m(r["pct_of_oracle"]) + ci(r["pct_of_oracle"]) + 1.2, y, "{:.1f}%".format(m(r["pct_of_oracle"])),
                        va="center", fontsize=8, color=INK)
    ax.axvline(100, color=INK2, linestyle=(0, (3, 3)), linewidth=1)
    ax.set_yticks(range(len(order)))
    ax.set_yticklabels([VARIANT_LABEL[v] for v in order])
    ax.set_ylim(len(order) - 0.5, -1.2)
    ax.text(99, -0.75, "Oracle = 100% (always sends the weak enemy)", ha="right", va="center", fontsize=7.5, color=INK2)
    ax.set_xlim(0, 105)
    ax.set_xlabel("Damage per spent cost, % of oracle (mean ± 95% CI)")
    ax.grid(axis="x", color=GRID, linewidth=0.6)
    ax.set_axisbelow(True)
    ax.tick_params(axis="y", length=0)
    fig.text(0.07, 0.75, "Ablation: what the rules and the learning each add", fontsize=12, weight="bold", color=INK, va="top")
    fig.text(0.07, 0.73, "Player weak to deer, AoE-heavy defense (so the hand-written rules correctly point to deer).",
             fontsize=8.5, color=INK2, va="top")
    if nver > 1:
        for vi, run in enumerate(versions):
            ax.add_patch(plt.Rectangle((1 + vi * 16, -0.86), 2.2, 0.22, color=SERIES[vi], clip_on=False))
            ax.text(4 + vi * 16, -0.75, run["meta"]["version"], fontsize=8, color=INK2, va="center")

    gaps = {r["variant"]: r["gap_closed_pct"] for r in cur["ablation"]}
    y = 0.33
    y = text_block(fig, 0.07, y, ["What this shows"], size=10, color=INK, weight="bold")
    y = text_block(fig, 0.07, y - 0.004, [
        "• Rules alone close {:.1f}% of the gap from random to the oracle; learning alone {:.1f}%; together {:.1f}%.".format(
            gaps["RulesOnly"], gaps["LearningOnly"], gaps["RulesPlusLearning"]),
        "  The combination does more than the two added up: the rules give a good first guess, learning corrects it.",
        "• The oracle is out of reach on purpose. Caps (rules 1.5x, learning 2x) keep the enemy mix varied",
        "  instead of sending only one enemy type, which would make the game repetitive.",
        "• Every variant spends exactly the level's cost budget, so the AI changes WHICH enemies come, not how hard",
        "  the level is.",
    ], size=8.5)

    # ---- Version table
    y -= 0.02
    y = text_block(fig, 0.07, y, ["Version history"], size=10, color=INK, weight="bold")
    cols = ["Version", "Saved", "Rules+learning\n% of oracle", "Gap closed", "Switch: belief\nflips after", "Edge cases", "Change"]
    xs = [0.07, 0.16, 0.30, 0.42, 0.52, 0.64, 0.73]
    for x, c in zip(xs, cols):
        fig.text(x, y - 0.002, c, fontsize=7.5, color=MUTED, va="top")
    y -= 0.035
    for run in hist[-8:]:
        a = {r["variant"]: r for r in run["ablation"]}["RulesPlusLearning"]
        sw = [r for r in run["strategy_switch"]["rows"] if abs(r["decay"] - run["meta"].get("decay", 0.85)) < 1e-6] or run["strategy_switch"]["rows"]
        e = run["edge_cases"]
        vals = [run["meta"]["version"], run["meta"]["saved"][:16], "{:.1f} ± {:.1f}%".format(m(a["pct_of_oracle"]), ci(a["pct_of_oracle"])),
                "{:.1f}%".format(a["gap_closed_pct"]), "{:.0f}s (median)".format(sw[0]["flip_median_s"]),
                "{}/{}".format(e["passed"], e["passed"] + e["failed"]), (lambda n: n if len(n) <= 34 else n[:33] + "…")(run["meta"].get("note") or "")]
        weight = "bold" if run["meta"]["version"] == meta["version"] else "normal"
        for x, v in zip(xs, vals):
            fig.text(x, y, v, fontsize=7.5, color=INK, va="top", weight=weight)
        y -= 0.02
    pdf.savefig(fig)
    plt.close(fig)


def page_vs_algorithm(pdf, cur):
    """AI (rules + learning) vs the algorithm alone (rules only, no learning), across every test."""
    fig = plt.figure(figsize=PAGE)
    fig.text(0.07, 0.95, "AI vs algorithm only", fontsize=14, weight="bold", color=INK, va="top")
    fig.text(0.07, 0.928, "Same rules in both. 'Algorithm only' = the hand-written rules with learning switched off.\n"
             "'AI' = the same rules plus online learning (Thompson sampling). Ratio = AI / algorithm only.",
             fontsize=8.5, color=INK2, va="top", linespacing=1.4)

    ab = {r["variant"]: r for r in cur["ablation"]}
    sw = cur["strategy_switch"]
    sw_row = [r for r in sw["rows"] if abs(r["decay"] - cur["meta"].get("decay", 0.85)) < 1e-6] or sw["rows"]
    wr = cur["wrong_rules"]
    rows = [
        ("Weak to deer, rules point to deer", "damage per cost, % of oracle", ab["RulesOnly"]["pct_of_oracle"], ab["RulesPlusLearning"]["pct_of_oracle"], "%"),
        ("Rules wrong (push stealth, weak to deer)", "deer share of cost", wr["rules_only_deer_pct"], wr["rules_learning_deer_pct"], "%"),
    ]
    if "rules_only_deer_share_after_pct" in sw:
        rows.append(("Weakness switches skunks → deer", "deer share, 60–90s after", sw["rules_only_deer_share_after_pct"][2], sw_row[0]["deer_share_after_pct"][2], "%"))
    if "combat" in cur:
        for r in cur["combat"]["rows"]:
            v = {x["variant"]: x["pressure"] for x in r["variants"]}
            rows.append(("Combat: " + r["loadout"], "pressure (damage per cost)", v["RulesOnly"], v["RulesPlusLearning"], ""))

    ax = fig.add_axes([0.37, 0.545, 0.56, 0.32])
    sig_count = 0
    worse_count = 0
    for yi, (name, metric, ro, rl, unit) in enumerate(rows):
        ratio = m(rl) / m(ro) if m(ro) else float("nan")
        sig = (m(rl) - ci(rl)) > (m(ro) + ci(ro))
        worse = (m(rl) + ci(rl)) < (m(ro) - ci(ro))
        sig_count += sig
        worse_count += worse
        ax.barh(yi, ratio, height=0.62, color=SERIES[0] if sig else NEUTRAL_BAR)
        ax.text(ratio + 0.03, yi, "{:.2f}x{}".format(ratio, " *" if sig else ""), va="center", fontsize=8, color=INK)
    ax.axvline(1.0, color=INK2, linestyle=(0, (3, 3)), linewidth=1)
    ax.set_yticks(range(len(rows)))
    ax.set_yticklabels([r[0] for r in rows], fontsize=8)
    ax.set_ylim(len(rows) - 0.5, -0.6)
    ax.set_xlim(0, max(m(r[3]) / m(r[2]) for r in rows) * 1.2)
    ax.set_xlabel("AI / algorithm only (x). Dashed line = no difference.\nBlue and * = AI clearly better (95% CIs don't overlap)", linespacing=1.4)
    ax.grid(axis="x", color=GRID, linewidth=0.6)
    ax.set_axisbelow(True)
    ax.tick_params(axis="y", length=0)

    y = 0.455
    y = text_block(fig, 0.07, y, ["Numbers behind the chart (mean ± 95% CI)"], size=10, color=INK, weight="bold")
    cols = ["Scenario", "Measure", "Algorithm only", "AI", "Ratio"]
    xs = [0.07, 0.38, 0.62, 0.76, 0.88]
    for x, c in zip(xs, cols):
        fig.text(x, y - 0.002, c, fontsize=7.5, color=MUTED, va="top")
    y -= 0.025
    for name, metric, ro, rl, unit in rows:
        prec = 1 if unit == "%" else 2
        fmt = "{:." + str(prec) + "f} ± {:." + str(prec) + "f}" + unit
        vals = [name, metric, fmt.format(m(ro), ci(ro)), fmt.format(m(rl), ci(rl)), "{:.2f}x".format(m(rl) / m(ro))]
        for x, v in zip(xs, vals):
            fig.text(x, y, v, fontsize=7.3, color=INK, va="top")
        y -= 0.019

    nw = {r["variant"]: r for r in cur["no_weakness"]}
    y -= 0.02
    y = text_block(fig, 0.07, y, ["What this shows"], size=10, color=INK, weight="bold")
    better = [r[0] for r in rows if (m(r[3]) - ci(r[3])) > (m(r[2]) + ci(r[2]))]
    worse = [r[0] for r in rows if (m(r[3]) + ci(r[3])) < (m(r[2]) - ci(r[2]))]
    same = [r[0] for r in rows if r[0] not in better and r[0] not in worse]
    lines = ["• Learning makes the AI clearly better than the algorithm alone in {} of {} scenarios, and clearly worse in {}.".format(
        len(better), len(rows), len(worse))]
    def wrapped(text):
        return textwrap.wrap(text, width=118, subsequent_indent="  ")
    lines += wrapped("• Clearly better: " + ("; ".join(better) if better else "none") + ".")
    if worse:
        lines += wrapped("• Clearly worse: " + "; ".join(worse) + ".")
    lines += wrapped("• No clear difference: " + ("; ".join(same) if same else "none") + ".")
    lines += [
        "• Learning matters most where the rules can't know the answer: the rules are wrong, the player changes strategy, or",
        "  how much a hole (like missing radar) actually pays off.",
        "• Where there is no gain, either the rules already found the hole (half-empty planet: they aim at the empty side), or the",
        "  defense collapses whatever is sent (AoE heavy), or the defense has no strong weakness to learn (balanced).",
    ]
    if "RulesOnly" in nw:
        lines.append("• With nothing to exploit, the AI stays as varied as the algorithm (mix entropy {:.3f} vs {:.3f}), so learning doesn't".format(
            m(nw["RulesPlusLearning"]["entropy"]), m(nw["RulesOnly"]["entropy"])))
        lines.append("  make the game repetitive.")
    text_block(fig, 0.07, y - 0.003, lines, size=8.2)
    pdf.savefig(fig)
    plt.close(fig)


def page2(pdf, cur):
    fig = plt.figure(figsize=PAGE)
    sw = cur["strategy_switch"]
    rows = sw["rows"]
    default = [r for r in rows if abs(r["decay"] - cur["meta"].get("decay", 0.85)) < 1e-6]
    r0 = default[0] if default else rows[0]

    fig.text(0.07, 0.95, "Adapting when the player changes strategy", fontsize=14, weight="bold", color=INK, va="top")
    fig.text(0.07, 0.928, "The player's weakness switches from skunks to deer at {}s.\nShare of spent cost per 15s step, mean ± 95% CI (evidence kept per 15s: {:.2f}).".format(
        sw["switch_at_s"], r0["decay"]), fontsize=8.5, color=INK2, va="top", linespacing=1.4)

    ax = fig.add_axes([0.10, 0.60, 0.83, 0.28])
    ts = r0["timeseries"]
    step = ts["bucket_s"]
    xs = [i * step + step / 2 for i in range(len(ts["deer_pct"]))]
    for key, color, label in (("skunk_pct", SERIES[1], "Skunks"), ("deer_pct", SERIES[0], "Deer")):
        ys = [m(s) for s in ts[key]]
        lo = [m(s) - ci(s) for s in ts[key]]
        hi = [m(s) + ci(s) for s in ts[key]]
        ax.fill_between(xs, lo, hi, color=color, alpha=0.15, linewidth=0)
        ax.plot(xs, ys, color=color, linewidth=2, marker="o", markersize=4)
        ax.text(xs[-1] + 2, ys[-1], label, color=INK, fontsize=8.5, va="center")
    ax.axvline(sw["switch_at_s"], color=INK2, linestyle=(0, (3, 3)), linewidth=1)
    ax.text(sw["switch_at_s"] + 2, ax.get_ylim()[1] * 0.97, "weakness switches\nskunks → deer", fontsize=7.5, color=INK2, va="top")
    ax.axvspan(0, 36, color=GRID, alpha=0.5, linewidth=0)
    ax.text(18, ax.get_ylim()[1] * 0.97, "no adaptation\n(first 20%)", fontsize=7.5, color=INK2, va="top", ha="center")
    ax.set_xlim(0, xs[-1] + 14)
    ax.set_xlabel("Time in level (s)")
    ax.set_ylabel("Share of spent cost (%)")
    ax.grid(axis="y", color=GRID, linewidth=0.6)
    ax.set_axisbelow(True)

    # decay table
    y = 0.53
    y = text_block(fig, 0.07, y, ["Forgetting rate trade-off"], size=10, color=INK, weight="bold")
    cols = ["Evidence kept\nper 15s", "Belief flips\n(median)", "Deer share\n60–90s after", "Reference (deer weak\nfrom the start)", "Recovered", "Steady case\n% of oracle"]
    xs2 = [0.07, 0.20, 0.33, 0.47, 0.65, 0.77]
    for x, c in zip(xs2, cols):
        fig.text(x, y - 0.002, c, fontsize=7.5, color=MUTED, va="top")
    y -= 0.04
    for r in rows:
        after = r["deer_share_after_pct"][2]
        rec = 100 * m(after) / m(r["reference_pct"]) if m(r["reference_pct"]) else float("nan")
        vals = ["{:.2f}{}".format(r["decay"], "  (default)" if r is r0 else ""), "{:.0f}s".format(r["flip_median_s"]),
                "{:.1f} ± {:.1f}%".format(m(after), ci(after)), "{:.1f} ± {:.1f}%".format(m(r["reference_pct"]), ci(r["reference_pct"])),
                "{:.0f}%".format(rec), "{:.1f} ± {:.1f}%".format(m(r["steady_pct_of_oracle"]), ci(r["steady_pct_of_oracle"]))]
        for x, v in zip(xs2, vals):
            fig.text(x, y, v, fontsize=8, color=INK, va="top", weight="bold" if r is r0 else "normal")
        y -= 0.022
    y = text_block(fig, 0.07, y - 0.005, [
        "Forgetting faster follows a change sooner but throws away evidence in the steady case. The default keeps most of the",
        "steady-case performance while catching up almost fully within 60–90s.",
    ], size=8.5)

    # wrong rules
    wr = cur["wrong_rules"]
    y -= 0.03
    y = text_block(fig, 0.07, y, ["When the hand-written rules are wrong"], size=10, color=INK, weight="bold")
    y = text_block(fig, 0.07, y - 0.003, ["No radar → the rules push stealth geese, but the player is really weak to deer."], size=8.5)
    ax2 = fig.add_axes([0.27, y - 0.13, 0.62, 0.11])
    labels = ["Rules only", "Rules + learning"]
    vals = [wr["rules_only_deer_pct"], wr["rules_learning_deer_pct"]]
    for i, s in enumerate(vals):
        ax2.barh(i, m(s), height=0.55, color=SERIES[0] if i == 1 else NEUTRAL_BAR, xerr=ci(s), error_kw=dict(ecolor=INK2, elinewidth=1, capsize=2))
        ax2.text(m(s) + ci(s) + 0.8, i, "{:.1f}%".format(m(s)), va="center", fontsize=8, color=INK)
    ax2.set_yticks([0, 1])
    ax2.set_yticklabels(labels)
    ax2.invert_yaxis()
    ax2.set_xlabel("Deer share of spent cost (%)")
    ax2.set_xlim(0, max(m(v) + ci(v) for v in vals) * 1.25)
    ax2.grid(axis="x", color=GRID, linewidth=0.6)
    ax2.set_axisbelow(True)
    ax2.tick_params(axis="y", length=0)
    text_block(fig, 0.07, y - 0.185, [
        "Learning overrules the wrong rule after a median of {:.0f}s from when adaptation starts ({} of {} runs never did).".format(
            wr["override_median_s"], wr["never"], cur["runs_per_test"]),
    ], size=8.5)
    pdf.savefig(fig)
    plt.close(fig)


def page3(pdf, cur):
    fig = plt.figure(figsize=PAGE)
    fig.text(0.07, 0.95, "Robustness", fontsize=14, weight="bold", color=INK, va="top")

    y = 0.91
    y = text_block(fig, 0.07, y, ["Player with no weakness: does the mix stay varied?"], size=10, color=INK, weight="bold")
    y -= 0.004
    for r in cur["no_weakness"]:
        y = text_block(fig, 0.09, y, ["{}: normalized entropy {:.3f} ± {:.3f} (1 = perfectly even), largest family {:.1f}%".format(
            VARIANT_LABEL[r["variant"]], m(r["entropy"]), ci(r["entropy"]), m(r["largest_share_pct"]))], size=8.5)
    y = text_block(fig, 0.09, y, ["→ With nothing to exploit, the AI doesn't lock onto one enemy type."], size=8.5)

    ec = cur["edge_cases"]
    y -= 0.025
    y = text_block(fig, 0.07, y, ["Edge cases: {} passed, {} failed".format(ec["passed"], ec["failed"])], size=10, color=INK, weight="bold")
    y -= 0.004
    for c in ec["cases"]:
        mark, color = ("PASS", GOOD) if c["ok"] else ("FAIL", CRITICAL)
        fig.text(0.09, y, mark, fontsize=7.5, color=color, weight="bold", va="top")
        fig.text(0.15, y, c["name"], fontsize=7.5, color=INK, va="top")
        y -= 0.016

    y -= 0.02
    y = text_block(fig, 0.07, y, ["The team's real assets (Level1.asset + enemy assets) through the Unity bridge"], size=10, color=INK, weight="bold")
    y -= 0.004
    for line in cur["integration"]:
        y = text_block(fig, 0.09, y, ["• " + line], size=7.5, gap=0.016)

    y -= 0.025
    y = text_block(fig, 0.07, y, ["How the tests work"], size=10, color=INK, weight="bold")
    text_block(fig, 0.07, y - 0.004, [
        "• The AI and LevelData run without Unity (Tools/EnemyAISim, small stand-ins for UnityEngine types).",
        "• Player model: each enemy family lets through cost × multiplier × random(0.5–1.5) damage. 'Weak to deer' = deer 2.0, others 0.2.",
        "• Each number is the mean over {} simulated levels with different random seeds; ± is the 95% confidence interval.".format(cur["runs_per_test"]),
        "• Oracle: knows the weakness from the start and always sends that enemy. It is the upper bound, not a goal.",
        "• Rerun:  ./run.sh all  then  python3 report.py --version <name> --note \"what changed\"",
    ], size=8.5)
    pdf.savefig(fig)
    plt.close(fig)


def page4(pdf, cur):
    if "combat" not in cur:
        return
    cb = cur["combat"]
    fig = plt.figure(figsize=PAGE)
    fig.text(0.07, 0.95, "Combat model: weakness comes from the towers, not assumed", fontsize=14, weight="bold", color=INK, va="top")
    fig.text(0.07, 0.928, "Towers and enemies use the README numbers (HP, damage, FOV, AoE/single/DoT, shields, stealth + radar, fuel use and drops,\n"
             "deer orbit/charge, shiny). 12-slot medium planet, rotating clockwise. {} levels per bar.".format(cb["runs"]),
             fontsize=8.5, color=INK2, va="top", linespacing=1.4)

    rows = cb["rows"]
    names = [r["loadout"] for r in rows]

    def pv(row, variant):
        return [v for v in row["variants"] if v["variant"] == variant][0]["pressure"]

    ax = fig.add_axes([0.27, 0.56, 0.66, 0.30])
    series = [("RulesOnly", "Rules only", SERIES[1]), ("RulesPlusLearning", "Rules + learning", SERIES[0])]
    h = 0.36
    for si, (key, label, color) in enumerate(series):
        for yi, row in enumerate(rows):
            base, val = pv(row, "Random"), pv(row, key)
            ratio = m(val) / m(base) if m(base) else float("nan")
            y = yi + (si - 0.5) * h
            ax.barh(y, ratio, height=h * 0.86, color=color)
            sig = (m(val) - ci(val)) > (m(base) + ci(base))
            ax.text(ratio + 0.03, y, "{:.2f}x{}".format(ratio, " *" if sig else ""), va="center", fontsize=7.5, color=INK)
    ax.axvline(1.0, color=INK2, linestyle=(0, (3, 3)), linewidth=1)
    ax.set_yticks(range(len(rows)))
    ax.set_yticklabels(names)
    ax.set_ylim(len(rows) - 0.5, -1.2)
    for si, (key, label, color) in enumerate(series):
        ax.add_patch(plt.Rectangle((0.05 + si * 0.55, -0.96), 0.06, 0.22, color=color, clip_on=False))
        ax.text(0.13 + si * 0.55, -0.85, label, fontsize=8, color=INK2, va="center")
    ax.set_xlim(0, max(m(pv(r, "RulesPlusLearning")) / m(pv(r, "Random")) for r in rows) * 1.25)
    ax.set_xlabel("Pressure vs a random enemy mix (x). Dashed line = random.  * = 95% CIs don't overlap")
    ax.grid(axis="x", color=GRID, linewidth=0.6)
    ax.set_axisbelow(True)
    ax.tick_params(axis="y", length=0)
    fig.text(0.07, 0.885, "Pressure = damage to the player's towers + planet per unit of enemy cost spent (after the first 20% of the level).",
             fontsize=8, color=MUTED, va="top")

    y = 0.50
    y = text_block(fig, 0.07, y, ["Per loadout"], size=10, color=INK, weight="bold")
    cols = ["Loadout", "Random", "Rules + learning", "Best single family\n(in hindsight)", "AI's mix (cost %)\nGeese/Stealth/Skunks/Deer"]
    xs = [0.07, 0.30, 0.43, 0.59, 0.77]
    for x, c in zip(xs, cols):
        fig.text(x, y - 0.002, c, fontsize=7.5, color=MUTED, va="top")
    y -= 0.04
    fam = cb["families"]
    for r in rows:
        best_i = max(range(4), key=lambda i: r["family_only_pressure"][i])
        vals = [r["loadout"], "{:.2f} ± {:.2f}".format(m(pv(r, "Random")), ci(pv(r, "Random"))),
                "{:.2f} ± {:.2f}".format(m(pv(r, "RulesPlusLearning")), ci(pv(r, "RulesPlusLearning"))),
                "{} {:.2f}".format(fam[best_i], r["family_only_pressure"][best_i]),
                " / ".join("{:.0f}".format(x) for x in r["rl_family_mix_pct"])]
        for x, v in zip(xs, vals):
            fig.text(x, y, v, fontsize=7.5, color=INK, va="top")
        y -= 0.02

    y -= 0.015
    y = text_block(fig, 0.07, y, ["What this shows (honestly)"], size=10, color=INK, weight="bold")
    y = text_block(fig, 0.07, y - 0.003, [
        "• Where the defense has a clear hole (no radar, half the planet empty, single-target towers), the AI puts clearly more",
        "  pressure on the player than a random mix. Learning adds most of the gain against no radar and single-target towers;",
        "  against the half-empty planet the rules already find the hole (aiming at the empty side).",
        "• Balanced defense: small gain, inside the noise. AoE heavy: no gain. That defense is short on fuel and collapses whatever",
        "  is sent, so the choice of enemy barely matters.",
        "• The best single family is far above the AI. The caps (rules 1.5x, learning 2x) keep the mix varied on purpose, so the",
        "  AI never floods one enemy type. Raising them is a game-design call, not a bug.",
        "• Tried and rejected: scoring each enemy against the running average instead of its own cost. It was worse in every",
        "  simple test and gave nothing here (Settings.RelativeReward, off).",
    ], size=8.2)

    y -= 0.015
    y = text_block(fig, 0.07, y, ["Assumptions (not in the README; change them in Combat.cs once the game has real values)"], size=9, color=INK, weight="bold")
    text_block(fig, 0.07, y - 0.003, [
        "Fire rate: Sniper 0.4/s, Laser SMG 2.5/s, Ballistica 0.5/s, Pest Trap 0.5/s, others 1/s.  Enemy attacks: geese every 1.5s, skunks 2s, deer charge 4s.",
        "Tower range 5 (planet radius 1, enemies stop at 3, orbit at 3.5), AoE radius 15 degrees, rotation 6 deg/s, starting fuel 60.",
        "Level budget 800, calibrated so a random enemy mix destroys about 3 of 12 towers on the balanced defense.",
        "An enemy hits the slot right below it (tower if there is one, else the planet); skunk AoE also hits both neighbour slots.",
    ], size=7.5, gap=0.016)
    pdf.savefig(fig)
    plt.close(fig)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--version", required=True, help="label for this run, e.g. v1.3")
    ap.add_argument("--note", default="", help="what changed in this version")
    ap.add_argument("--decay", type=float, default=0.85, help="the AI's current DecayPerStep (default 0.85)")
    ap.add_argument("--results", default=os.path.join(HERE, "results.json"))
    ap.add_argument("--commit", default=None, help="commit id to print (default: git rev-parse HEAD)")
    args = ap.parse_args()

    with open(args.results) as f:
        cur = json.load(f)
    cur["meta"] = {"version": args.version, "note": args.note, "commit": args.commit or git_commit(), "decay": args.decay,
                   "saved": datetime.now().strftime("%Y-%m-%d %H:%M")}

    os.makedirs(os.path.join(HERE, "history"), exist_ok=True)
    with open(os.path.join(HERE, "history", args.version + ".json"), "w") as f:
        json.dump(cur, f)
    hist = load_history()

    out = os.path.join(HERE, "EnemyAI_Report_{}.pdf".format(args.version))
    with PdfPages(out) as pdf:
        page1(pdf, cur, hist)
        page_vs_algorithm(pdf, cur)
        page2(pdf, cur)
        page3(pdf, cur)
        page4(pdf, cur)
        info = pdf.infodict()
        info["Title"] = "Enemy AI test report " + args.version
    print("Wrote " + out)


if __name__ == "__main__":
    main()
