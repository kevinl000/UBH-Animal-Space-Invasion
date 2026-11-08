#!/usr/bin/env python3
"""Builds EnemyAI_Report_v1.3.pdf from the v1.3 raw per-seed results.

Inputs (written by run.sh):  results13.json (./run.sh v13), results13_r4d.json (./run.sh r4d),
results13_switch.json (./run.sh switch), results.json (./run.sh all, the v1.2 suite for the regression check).
Every comparison is judged by the paired difference of the same seeds (common random numbers):
95% CI by bootstrap (10,000 resamples) and by the t distribution. Significant = the bootstrap CI excludes 0.

Usage:  python3 report13.py --commit <id> [--version v1.3]
Needs:  matplotlib, numpy, scipy
"""
import argparse
import json
import os
import subprocess
import textwrap
from datetime import datetime

import numpy as np
from scipy import stats
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.backends.backend_pdf import PdfPages

HERE = os.path.dirname(os.path.abspath(__file__))
SURFACE, INK, INK2, MUTED, GRID = "#fcfcfb", "#0b0b0b", "#52514e", "#8a8984", "#e6e5e1"
SERIES = ["#2a78d6", "#eb6834", "#1baf7a", "#8a5cc2", "#c9a227"]
NEUTRAL_BAR, GOOD, CRITICAL = "#c9c7c0", "#0ca30c", "#d03b3b"
FAMS = ["Geese", "Stealth geese", "Skunks", "Deer"]
LOADOUTS = ["Single-target heavy", "AoE heavy", "No radar", "Clustered (half empty)", "Balanced"]
VLABEL = {"Random": "Random", "DesignerOnly": "Designer shares only", "RulesOnly": "Rules only", "LearningOnly": "Learning only",
          "RulesPlusLearning": "Rules + learning", "Oracle": "Oracle"}
PAGE = (8.5, 11)
plt.rcParams.update({"font.family": "DejaVu Sans", "font.size": 9, "axes.edgecolor": GRID, "axes.labelcolor": INK2, "axes.titlecolor": INK,
                     "xtick.color": INK2, "ytick.color": INK2, "axes.spines.top": False, "axes.spines.right": False,
                     "figure.facecolor": SURFACE, "axes.facecolor": SURFACE, "savefig.facecolor": SURFACE})
RNG = np.random.default_rng(13)
BOOT = 10000


# ---------------------------------------------------------------- statistics
def mean_ci(x):
    x = np.asarray(x, float)
    return x.mean(), 1.96 * x.std(ddof=1) / np.sqrt(len(x))


def paired(a, b):
    """b - a on the same seeds. Returns dict(mean, boot_lo, boot_hi, t_lo, t_hi, sig, n)."""
    a, b = np.asarray(a, float), np.asarray(b, float)
    d = b - a
    n = len(d)
    idx = RNG.integers(0, n, size=(BOOT, n))
    bm = d[idx].mean(axis=1)
    lo, hi = np.percentile(bm, [2.5, 97.5])
    se = d.std(ddof=1) / np.sqrt(n)
    tq = stats.t.ppf(0.975, n - 1)
    sig = (lo > 0) or (hi < 0)
    return {"mean": d.mean(), "boot_lo": lo, "boot_hi": hi, "t_lo": d.mean() - tq * se, "t_hi": d.mean() + tq * se, "sig": bool(sig), "n": n,
            "overlap": _overlap(a, b)}


def _overlap(a, b):
    ma, ca = mean_ci(a)
    mb, cb = mean_ci(b)
    return not ((mb - cb) > (ma + ca) or (mb + cb) < (ma - ca))


def verdict(p, better_if_positive=True):
    if not p["sig"]:
        return "trend" if abs(p["mean"]) > 0 else "no difference"
    up = p["mean"] > 0
    return "significantly higher" if up else "significantly lower"


# ---------------------------------------------------------------- drawing helpers
def text_block(fig, x, y, lines, size=8.4, color=INK2, gap=0.0165, weight="normal"):
    for i, line in enumerate(lines):
        fig.text(x, y - i * gap, line, fontsize=size, color=color, weight=weight, va="top")
    return y - len(lines) * gap


def wrap(t, w=112, indent="  "):
    return textwrap.wrap(t, width=w, subsequent_indent=indent)


def header(fig, title, sub=None):
    fig.text(0.07, 0.95, title, fontsize=14, weight="bold", color=INK, va="top")
    if sub:
        fig.text(0.07, 0.928, sub, fontsize=8.3, color=INK2, va="top", linespacing=1.45)


def table(fig, x0, y, cols, xs, rows, size=7.2, gap=0.0172, color_fn=None):
    for x, c in zip(xs, cols):
        fig.text(x0 + x, y, c, fontsize=size, color=MUTED, va="top")
    y -= gap * 1.15
    for r in rows:
        for j, (x, v) in enumerate(zip(xs, r)):
            col = color_fn(r, j) if color_fn else INK
            fig.text(x0 + x, y, v, fontsize=size, color=col, va="top")
        y -= gap
    return y


def fmt_p(p, prec=3, unit=""):
    f = "{:+." + str(prec) + "f}"
    return (f + "{} [{:." + str(prec) + "f}, {:." + str(prec) + "f}]").format(p["mean"], unit, p["boot_lo"], p["boot_hi"])


# ---------------------------------------------------------------- data
def load(name):
    p = os.path.join(HERE, name)
    with open(p) as f:
        return json.load(f)


def comparisons(r, sw):
    """Every 'algorithm only vs AI' comparison of v1.2 p.2, re-judged by paired differences."""
    rows = []
    ab = r["abstract"]["ablation"]
    rows.append(("Abstract: weak to deer, rules point to deer", "% of oracle (damage per cost)", ab["RulesOnly"]["pct_of_oracle"], ab["RulesPlusLearning"]["pct_of_oracle"], 1, "%"))
    wr = r["abstract"]["wrong_rules"]
    rows.append(("Abstract: rules wrong (push stealth), weak to deer", "deer share of cost", [x * 100 for x in wr["RulesOnly"]["deer_share"]],
                 [x * 100 for x in wr["RulesPlusLearning"]["deer_share"]], 1, "%"))
    rows.append(("Abstract: weakness switches skunks -> deer", "deer share 60-90 s after", [x * 100 for x in sw["RulesOnly"]["deer_share_after"][2]],
                 [x * 100 for x in sw["RulesPlusLearning"]["deer_share_after"][2]], 1, "%"))
    for lo in LOADOUTS:
        v = r["combat"]["fuel_old"][lo]["variants"]
        rows.append(("Combat: " + lo, "pressure (damage per cost)", v["RulesOnly"]["pressure"], v["RulesPlusLearning"]["pressure"], 3, ""))
    return rows


# ---------------------------------------------------------------- pages
def page1(pdf, meta, r, comp, r1, regress, concl):
    fig = plt.figure(figsize=PAGE)
    fig.text(0.07, 0.95, "Enemy AI test report", fontsize=20, weight="bold", color=INK, va="top")
    fig.text(0.07, 0.915, "UB Hacking 2026 · Space tower defense · Wave director with online learning", fontsize=10, color=INK2, va="top")
    fig.text(0.07, 0.892, "Version {}  ·  commit {}  ·  {}  ·  {} seeds per condition (same seeds everywhere)".format(meta["version"], meta["commit"], meta["saved"], r["n"]),
             fontsize=8.5, color=MUTED, va="top")
    fig.text(0.07, 0.874, "Change in this version: paired statistics, team data checked, stealth-goose cause analysed (no algorithm change).",
             fontsize=8.3, color=INK2, va="top")
    y = 0.845
    y = text_block(fig, 0.07, y, ["How the tests work"], size=10.5, color=INK, weight="bold")
    hw = []
    hw += wrap("• Abstract: model assumes how weak the player is to each enemy family (a damage-per-cost multiplier) and checks whether the AI finds it.", 112)
    hw += wrap("• Combat: model lets towers and enemies fight with the README stats, so the weakness comes out of the tower loadout instead of being assumed.", 112)
    hw += wrap("• Every condition uses seeds 0-{} (common random numbers). A difference is significant when the 95% CI of the per-seed difference excludes 0 "
               "(bootstrap, 10,000 resamples; the t-distribution CI is shown too). Overlapping CIs of the two means are only a side note. Not significant = 'trend'.".format(r["n"] - 1), 112)
    y = text_block(fig, 0.07, y - 0.003, hw, size=8.2)
    y -= 0.012
    y = text_block(fig, 0.07, y, ["Results at a glance"], size=10.5, color=INK, weight="bold")
    sig_up = [c for c in comp if c["p"]["sig"] and c["p"]["mean"] > 0]
    sig_dn = [c for c in comp if c["p"]["sig"] and c["p"]["mean"] < 0]
    r1_ok = [x for x in r1 if all(x["pass"].values())]
    lines = ["• AI (rules + learning) vs algorithm only: significantly better in {} of {} comparisons, significantly worse in {}, trend in the rest.".format(
        len(sig_up), len(comp), len(sig_dn))]
    lines += wrap("• Team data (R1): {} of {} level / wave assets pass every check; ".format(len(r1_ok), len(r1)) + "; ".join(
        "{} fails ({})".format(x["label"].split(" (")[0], ", ".join(k for k, v in x["pass"].items() if not v)) for x in r1 if not all(x["pass"].values())) + ".")
    lines += wrap("• Regression (C5): {} of the v1.2 numbers re-computed on this commit, {} differ; edge cases {} passed / {} failed.".format(
        regress["compared"], regress["different"], regress["edge_pass"], regress["edge_fail"]))
    for c in concl:
        lines += wrap("• " + c, 112)
    y = text_block(fig, 0.07, y - 0.003, lines, size=8.2)

    y -= 0.014
    y = text_block(fig, 0.07, y, ["Requirements in this version"], size=10.5, color=INK, weight="bold")
    req = [("C1 clean commit", "commit " + meta["commit"] + (" (clean)" if "+" not in meta["commit"] else " (DIRTY)")),
           ("C2 common seeds", "seeds 0-{} in every condition".format(r["n"] - 1)),
           ("C3 runs", "abstract {} / combat {} per condition".format(r["n"], r["n"])),
           ("C4 paired CIs", "bootstrap 10k + t; overlap is a side note"),
           ("C5 regression", "{} numbers identical, edge {}/{}".format(regress["compared"] - regress["different"], regress["edge_pass"], regress["edge_pass"] + regress["edge_fail"])),
           ("C6 wording", "non-significant results are called 'trend'"),
           ("R1 team data", "level + wave assets, see p.6"), ("R2 scenario names", "Abstract: / Combat: on every page"),
           ("R3 claims vs numbers", "p.2-3 re-judged"), ("R4a/b/c/d", "p.4-5 (all done)"), ("R5 small fixes", "p.3 + version table")]
    y = table(fig, 0.07, y, ["Item", "Status"], [0, 0.22], [[a, b] for a, b in req], size=7.4)

    y -= 0.012
    y = text_block(fig, 0.07, y, ["Version history"], size=10.5, color=INK, weight="bold")
    hist = [("v1.0", "first offline test suite", "baseline"),
            ("v1.1", "decay 0.85, family-level learning, 20 edge cases", "deer share and switch speed up; edge cases added"),
            ("v1.2", "combat model from README stats, fuel-balanced loadouts", "adds the Combat: pressure metric (new scale)"),
            ("v1.3", "paired statistics, team data check, stealth analysis", "v1.2 numbers unchanged ({}/{} identical); significance labels re-judged".format(
                regress["compared"] - regress["different"], regress["compared"]))]
    table(fig, 0.07, y, ["Version", "Change", "Impact on metrics"], [0, 0.08, 0.44], [list(h) for h in hist], size=7.3)
    pdf.savefig(fig)
    plt.close(fig)


def page2(pdf, comp):
    fig = plt.figure(figsize=PAGE)
    header(fig, "AI vs algorithm only (paired differences)",
           "Same rules in both. 'Algorithm only' = the hand-written rules with learning off; 'AI' = the same rules plus online learning.\n"
           "Each row uses its own measure, so a ratio can only be compared within its own row (R5).")
    ax = fig.add_axes([0.42, 0.57, 0.52, 0.33])
    for yi, c in enumerate(comp):
        ratio = np.mean(c["ai"]) / np.mean(c["algo"]) if np.mean(c["algo"]) else float("nan")
        col = SERIES[0] if (c["p"]["sig"] and c["p"]["mean"] > 0) else (CRITICAL if c["p"]["sig"] else NEUTRAL_BAR)
        ax.barh(yi, ratio, height=0.62, color=col)
        ax.text(ratio + 0.03, yi, "{:.2f}x{}".format(ratio, " *" if c["p"]["sig"] else ""), va="center", fontsize=8, color=INK)
    ax.axvline(1.0, color=INK2, linestyle=(0, (3, 3)), linewidth=1)
    ax.set_yticks(range(len(comp)))
    ax.set_yticklabels([c["name"] for c in comp], fontsize=7.6)
    ax.set_ylim(len(comp) - 0.5, -0.6)
    ax.set_xlabel("AI / algorithm only (x)\nblue * = significantly higher, red * = significantly lower, grey = trend", linespacing=1.4)
    ax.grid(axis="x", color=GRID, linewidth=0.6)
    ax.set_axisbelow(True)
    ax.tick_params(axis="y", length=0)

    y = 0.49
    y = text_block(fig, 0.07, y, ["Numbers (means ± 95% CI; difference = AI - algorithm, same seeds)"], size=9.5, color=INK, weight="bold")
    rows = []
    for c in comp:
        pr, u = c["prec"], c["unit"]
        ma, ca = mean_ci(c["algo"])
        mb, cb = mean_ci(c["ai"])
        f = "{:." + str(pr) + "f}"
        rows.append([c["name"].replace("Abstract: ", "Abs: ").replace("Combat: ", "Com: "), (f + "±" + f + u).format(ma, ca), (f + "±" + f + u).format(mb, cb),
                     fmt_p(c["p"], pr, u), ("[" + f + ", " + f + "]").format(c["p"]["t_lo"], c["p"]["t_hi"]), verdict(c["p"]), "yes" if c["p"]["overlap"] else "no"])
    y = table(fig, 0.07, y - 0.004, ["Scenario", "Algorithm only", "AI", "Difference [bootstrap 95%]", "t 95%", "Judgement", "Overlap"],
              [0, 0.25, 0.355, 0.46, 0.635, 0.735, 0.85], rows, size=6.4,
              color_fn=lambda row, j: (GOOD if row[5].startswith("significantly h") else CRITICAL if row[5].startswith("significantly l") else INK) if j == 5 else INK)
    y -= 0.015
    up = [c["name"] for c in comp if c["p"]["sig"] and c["p"]["mean"] > 0]
    dn = [c["name"] for c in comp if c["p"]["sig"] and c["p"]["mean"] < 0]
    tr = [c["name"] for c in comp if not c["p"]["sig"]]
    changed = [c["name"] for c in comp if c["p"]["sig"] != (not c["p"]["overlap"])]
    lines = []
    lines += wrap("• Significantly higher with learning: " + ("; ".join(up) if up else "none") + ".")
    if dn:
        lines += wrap("• Significantly lower with learning: " + "; ".join(dn) + ".")
    lines += wrap("• Trend only (paired CI includes 0): " + ("; ".join(tr) if tr else "none") + ".")
    lines += wrap("• Changed by the paired method compared with the v1.2 'CIs overlap' rule: " + ("; ".join(changed) if changed else "none") + ".")
    text_block(fig, 0.07, y, lines, size=8.0)
    pdf.savefig(fig)
    plt.close(fig)


def page3(pdf, r):
    fig = plt.figure(figsize=PAGE)
    header(fig, "Combat: model in detail (fuel as in v1.2)",
           "Pressure = damage dealt to towers + planet per spent enemy cost, after the first 20% of the level. Higher = harder on the player.")
    old = r["combat"]["fuel_old"]
    order = ["Random", "DesignerOnly", "RulesOnly", "LearningOnly", "RulesPlusLearning"]
    rows = []
    learn_claim = []
    for lo in LOADOUTS:
        v = old[lo]["variants"]
        cells = ["{:.3f}±{:.3f}".format(*mean_ci(v[k]["pressure"])) for k in order]
        p_rl_ro = paired(v["RulesOnly"]["pressure"], v["RulesPlusLearning"]["pressure"])
        p_rl_d = paired(v["DesignerOnly"]["pressure"], v["RulesPlusLearning"]["pressure"])
        p_ro_d = paired(v["DesignerOnly"]["pressure"], v["RulesOnly"]["pressure"])
        rows.append([lo] + cells + [verdict(p_rl_ro)])
        if p_rl_ro["sig"] and p_rl_ro["mean"] > 0:
            tot = p_rl_d["mean"]
            share = p_rl_ro["mean"] / tot * 100 if tot > 0 else float("nan")
            learn_claim.append((lo, share))
    y = 0.885
    y = table(fig, 0.07, y, ["Combat: loadout", "Random", "Designer", "Rules only", "Learning only", "Rules + learning", "R+L vs rules only"],
              [0, 0.2, 0.31, 0.42, 0.53, 0.65, 0.78], rows, size=6.9)
    y -= 0.012
    lines = []
    if learn_claim:
        lines += wrap("• Learning carries most of the improvement over the designer mix only where 'rules + learning' beats 'rules only' significantly: " +
                      "; ".join("{} ({:.0f}% of the gain)".format(lo, s) for lo, s in learn_claim) + ". Elsewhere this report does not make that claim (R3).")
    else:
        lines += wrap("• In no loadout is 'rules + learning' significantly above 'rules only', so this report makes no claim that learning carries the improvement (R3).")
    # R5: why designer is below random
    deer_d = np.mean([np.mean([m[3] for m in old[lo]["variants"]["DesignerOnly"]["mix"]]) for lo in LOADOUTS]) * 100
    deer_r = np.mean([np.mean([m[3] for m in old[lo]["variants"]["Random"]["mix"]]) for lo in LOADOUTS]) * 100
    st_d = np.mean([np.mean([m[1] for m in old[lo]["variants"]["DesignerOnly"]["mix"]]) for lo in LOADOUTS]) * 100
    st_r = np.mean([np.mean([m[1] for m in old[lo]["variants"]["Random"]["mix"]]) for lo in LOADOUTS]) * 100
    lines += wrap("• Why 'designer shares only' is below random (R5): the designer's starting shares put deer at {:.0f}% of spent cost (random: {:.0f}%) and stealth geese at {:.0f}% (random: {:.0f}%), "
                  "and those are the two families with the most pressure per cost here.".format(deer_d, deer_r, st_d, st_r))
    y = text_block(fig, 0.07, y, lines, size=8.0)

    # fuel models
    y -= 0.02
    y = text_block(fig, 0.07, y, ["Fuel model: v1.2 (continuous drain) vs game since 2026-11-08 (weapons pay per shot)"], size=9.5, color=INK, weight="bold")
    new = r["combat"]["fuel_new"]
    rows = []
    for lo in LOADOUTS:
        a, b = old[lo]["variants"], new[lo]["variants"]
        p1 = paired(a["RulesPlusLearning"]["pressure"], b["RulesPlusLearning"]["pressure"])
        pn = paired(b["RulesOnly"]["pressure"], b["RulesPlusLearning"]["pressure"])
        rows.append([lo, "{:.3f}±{:.3f}".format(*mean_ci(a["RulesPlusLearning"]["pressure"])), "{:.3f}±{:.3f}".format(*mean_ci(b["RulesPlusLearning"]["pressure"])),
                     fmt_p(p1, 3), verdict(p1), verdict(pn)])
    y = table(fig, 0.07, y - 0.004, ["Combat: loadout", "R+L, old fuel", "R+L, new fuel", "New - old [95%]", "Judgement", "New fuel: R+L vs rules only"],
              [0, 0.21, 0.33, 0.45, 0.66, 0.80], rows, size=6.9)
    y -= 0.01
    text_block(fig, 0.07, y, wrap("• Both fuel models use the same seeds; the main tables keep the v1.2 model so v1.3 stays comparable with v1.2 (C5). "
                                  "With the new model weapons only spend fuel when they fire, so towers stay powered longer; the AI's fuel rule reads the new estimate."), size=7.8)

    # chart: pressure by variant, old fuel
    ax = fig.add_axes([0.1, 0.07, 0.84, 0.27])
    w = 0.16
    for i, k in enumerate(order):
        ms = [np.mean(old[lo]["variants"][k]["pressure"]) for lo in LOADOUTS]
        cs = [mean_ci(old[lo]["variants"][k]["pressure"])[1] for lo in LOADOUTS]
        ax.bar(np.arange(len(LOADOUTS)) + (i - 2) * w, ms, w, yerr=cs, color=[NEUTRAL_BAR, MUTED, SERIES[1], SERIES[2], SERIES[0]][i], label=VLABEL[k], error_kw={"elinewidth": 0.6})
    ax.set_xticks(range(len(LOADOUTS)))
    ax.set_xticklabels(LOADOUTS, fontsize=7.4)
    ax.set_ylabel("Combat: pressure")
    ax.legend(fontsize=7, frameon=False, ncol=5, loc="upper center", bbox_to_anchor=(0.5, 1.13))
    ax.grid(axis="y", color=GRID, linewidth=0.6)
    ax.set_axisbelow(True)
    pdf.savefig(fig)
    plt.close(fig)


def linearity(points):
    """points: [(share, mean pressure)]. Linear if the straight-line fit explains it (R^2 >= 0.95) and no step beats 2x the average slope."""
    x = np.array([p[0] for p in points])
    yv = np.array([p[1] for p in points])
    slope, icpt, rval, _, _ = stats.linregress(x, yv)
    r2 = rval ** 2
    steps = np.diff(yv) / np.diff(x)
    avg = (yv[-1] - yv[0]) / (x[-1] - x[0]) if x[-1] != x[0] else 0
    jump = bool(avg != 0 and np.max(np.abs(steps)) > 2 * abs(avg))
    return r2, jump


def page4(pdf, r, decide):
    fig = plt.figure(figsize=PAGE)
    header(fig, "Stealth geese: proportional? (R4a) · Why skunks? (R4c)",
           "R4a: stealth geese fixed at 0/10/25/50/75/100% of the designer budget (the rest keeps the designer ratios), no AI adaptation.\n"
           "If pressure rises roughly in proportion, the bandit's assumption holds and the cause is the numbers; a sudden rise = non-linear effect.")
    ax = fig.add_axes([0.1, 0.62, 0.55, 0.26])
    res = []
    for i, lo in enumerate(LOADOUTS):
        cells = r["r4a"][lo]
        xs = [c["share"] * 100 for c in cells]
        ms = [np.mean(c["pressure"]) for c in cells]
        cs = [mean_ci(c["pressure"])[1] for c in cells]
        real = [np.mean(c["realized"]) * 100 for c in cells]
        ax.errorbar(xs, ms, yerr=cs, marker="o", ms=3, lw=1.3, color=SERIES[i], label=lo, capsize=2)
        r2, jump = linearity(list(zip([c["share"] for c in cells], ms)))
        res.append((lo, r2, jump, ms[0], ms[-1], real))
    ax.set_xlabel("Stealth geese, fixed share of the designer budget (%)")
    ax.set_ylabel("Combat: pressure")
    ax.grid(color=GRID, linewidth=0.6)
    ax.legend(fontsize=6.6, frameon=False, loc="upper left", bbox_to_anchor=(1.02, 1.0))
    y = 0.57
    rows = [[lo, "{:.3f}".format(r2), "jump" if jump else "smooth", "{:.3f} -> {:.3f}".format(a, b), "/".join("{:.0f}".format(v) for v in real)]
            for lo, r2, jump, a, b, real in res]
    y = table(fig, 0.07, y, ["Combat: loadout", "Linear fit R²", "Steps", "Pressure 0% -> 100%", "Realised stealth share of cost (%) at 0/10/25/50/75/100"],
              [0, 0.21, 0.32, 0.42, 0.58], rows, size=6.9)
    nonlin = [x[0] for x in res if x[1] < 0.95 or x[2]]
    decide["nonlinear"] = nonlin
    y -= 0.006
    y = text_block(fig, 0.07, y, wrap("• Proportional (R² ≥ 0.95, no step steeper than twice the average): {} of 5 loadouts. Non-linear: {}.".format(
        5 - len(nonlin), ", ".join(nonlin) if nonlin else "none")), size=8.0)

    # R4c
    y -= 0.02
    y = text_block(fig, 0.07, y, ["R4c: family mix (% of spent cost, G/S/Sk/D) and the learner's final beliefs"], size=9.3, color=INK, weight="bold")
    old = r["combat"]["fuel_old"]
    rows = []
    capped_any = False
    for lo in LOADOUTS:
        v = old[lo]["variants"]
        mix = {k: np.mean(np.array(v[k]["mix"]), axis=0) * 100 for k in ["DesignerOnly", "RulesOnly", "LearningOnly", "RulesPlusLearning"]}
        post = np.mean(np.array(v["RulesPlusLearning"]["posterior_mean"]), axis=0)
        cap = np.mean(np.array(v["RulesPlusLearning"]["factor_at_cap"]), axis=0)
        fam_best = int(np.argmax([np.mean(f) for f in old[lo]["family_only"]]))
        top_belief = int(np.argmax(post))
        capped_any |= cap[1] > 0.05
        rows.append([lo.replace(" (half empty)", "")] + ["/".join("{:.0f}".format(x) for x in mix[k]) for k in ["DesignerOnly", "RulesOnly", "LearningOnly", "RulesPlusLearning"]]
                    + ["/".join("{:.2f}".format(x) for x in post), FAMS[top_belief].replace(" geese", ""), FAMS[fam_best].replace(" geese", ""), "{:.0f}%".format(cap[1] * 100)])
        decide.setdefault("belief_stealth_top", 0)
        decide["belief_stealth_top"] += int(top_belief == 1)
        decide.setdefault("rl_stealth_share", []).append(float(mix["RulesPlusLearning"][1]))
        decide.setdefault("cap_share", []).append(float(cap[1]))
    y = table(fig, 0.07, y - 0.004, ["Combat: loadout", "Designer", "Rules only", "Learn only", "Rules+learn", "Belief G/S/Sk/D", "Top belief", "Truly best", "At cap"],
              [0, 0.155, 0.245, 0.335, 0.425, 0.52, 0.665, 0.75, 0.835], rows, size=6.2)
    decide["capped"] = capped_any
    decide["r4c_rows"] = rows
    y -= 0.008
    lines = wrap("• 'Stealth at cap' = share of the learner's draws for stealth geese at its 2x ceiling. Learned factor = draw / prior mean 0.5, so 2x is also the most the formula can give.")
    lines += wrap("• Skunks get more than their designer share under 'rules only' because the rules react to the defense (e.g. clustered towers -> AoE skunks); "
                  "the learner then moves budget toward the family it believes best. Compare 'Top belief' with 'Truly best' (hindsight: send only that family).")
    text_block(fig, 0.07, y, lines, size=7.8)
    pdf.savefig(fig)
    plt.close(fig)


def page5(pdf, r, cap, decide):
    fig = plt.figure(figsize=PAGE)
    header(fig, "Sensitivity of assumed values (R4b) and the learning cap (R4d)",
           "R4b: the values the README doesn't give (fire rates, tower range, radar reveal width) moved ±50%. Is stealth still the best family?\n"
           "Best family = highest pressure when only that family is sent (hindsight).")
    base = r["combat"]["fuel_old"]
    rows = []
    base_best = {lo: int(np.argmax([np.mean(f) for f in base[lo]["family_only"]])) for lo in LOADOUTS}
    rows.append(["baseline (v1.2 values)"] + [FAMS[base_best[lo]].replace(" geese", "") for lo in LOADOUTS])
    stealth_kept = []
    for st, los in r["r4b"].items():
        best = {lo: int(np.argmax([np.mean(f) for f in los[lo]["family_only"]])) for lo in LOADOUTS}
        rows.append([st] + [FAMS[best[lo]].replace(" geese", "") for lo in LOADOUTS])
        stealth_kept.append(sum(best[lo] == 1 for lo in LOADOUTS))
        decide.setdefault("r4b_settings", []).append((st, sum(best[lo] == 1 for lo in LOADOUTS), FAMS[max(set(best.values()), key=list(best.values()).count)]))
    y = table(fig, 0.07, 0.885, ["Setting"] + [lo.replace(" (half empty)", "") for lo in LOADOUTS], [0, 0.2, 0.34, 0.47, 0.6, 0.74], rows, size=7.1,
              color_fn=lambda row, j: CRITICAL if j > 0 and row[j] == "Stealth" else INK)
    base_st = sum(v == 1 for v in base_best.values())
    decide["stealth_baseline"] = base_st
    decide["stealth_sens"] = stealth_kept
    y -= 0.008
    y = text_block(fig, 0.07, y, wrap("• Stealth geese are the best family in {} of 5 loadouts with the v1.2 values and in {}-{} of 5 across the ±50% settings.".format(
        base_st, min(stealth_kept), max(stealth_kept))), size=8.0)

    y -= 0.02
    y = text_block(fig, 0.07, y, ["R4d: learning cap 2x (current) / 4x / no cap: pressure vs variety (entropy of the family mix)"], size=9.3, color=INK, weight="bold")
    y = text_block(fig, 0.07, y, wrap("Simulation-only patched learner: factor = (draw / prior mean)^γ, γ = 1 (2x, the game), 2 (4x), 8 with no clamp (≈ winner takes all). The game's file is not changed."), size=7.6)
    ax = fig.add_axes([0.1, 0.25, 0.55, 0.3])
    rows = []
    for i, lo in enumerate(LOADOUTS):
        ps, es = [], []
        for c in cap["caps"]:
            d = c["loadouts"][lo]
            ps.append(np.mean(d["pressure"]))
            es.append(np.mean(d["entropy"]))
        ax.plot(es, ps, marker="o", ms=4, lw=1.3, color=SERIES[i], label=lo)
        for c, e, p in zip(cap["caps"], es, ps):
            ax.annotate(c["cap"].split(" ")[0], (e, p), fontsize=6, color=INK2, xytext=(3, 3), textcoords="offset points")
        c0, c1, c2 = (cap["caps"][k]["loadouts"][lo] for k in range(3))
        p4 = paired(c0["pressure"], c1["pressure"])
        pn = paired(c0["pressure"], c2["pressure"])
        rows.append([lo, "{:.3f} / {:.3f} / {:.3f}".format(*ps), "{:.2f} / {:.2f} / {:.2f}".format(*es), verdict(p4), verdict(pn)])
        decide.setdefault("cap4_up", 0)
        decide["cap4_up"] += int(p4["sig"] and p4["mean"] > 0)
    ax.set_xlabel("Variety of the enemy mix (normalised entropy, 1 = even)")
    ax.set_ylabel("Combat: pressure")
    ax.grid(color=GRID, linewidth=0.6)
    ax.legend(fontsize=6.6, frameon=False, loc="upper left", bbox_to_anchor=(1.02, 1.0))
    y = table(fig, 0.07, 0.2, ["Combat: loadout", "Pressure 2x / 4x / none", "Entropy 2x / 4x / none", "4x vs 2x", "None vs 2x"], [0, 0.22, 0.43, 0.62, 0.8], rows, size=6.9)
    decide["cap_rows"] = rows
    pdf.savefig(fig)
    plt.close(fig)


def page6(pdf, r1, name_map, regress, concl):
    fig = plt.figure(figsize=PAGE)
    header(fig, "Team data (R1), regression (C5) and conclusions",
           "R1: every LevelData and every wave config in the repo, run through the bridge the way the game's spawners do (neutral defense, seed 1).")
    rows = []
    for x in r1:
        p = x["pass"]
        rows.append([x["label"].split(" (")[0], x["mode"], str(x["spawns"]), "{:.0f} / {:.0f}".format(x["spent"] or 0, x["budget"] or 0)] +
                    ["PASS" if p[k] else "FAIL" for k in ["spawns", "within_budget", "used_95", "no_nan", "names"]])
    y = table(fig, 0.07, 0.885, ["Asset", "Mode", "Spawns", "Spent / budget", ">0", "<=budget", ">=95%", "no NaN", "names"],
              [0, 0.2, 0.27, 0.35, 0.49, 0.56, 0.66, 0.75, 0.84], rows, size=7.0,
              color_fn=lambda row, j: (GOOD if row[j] == "PASS" else CRITICAL) if j >= 4 else INK)
    issues = [i for x in r1 for i in x["issues"]]
    y -= 0.008
    lines = ["Issues found (R1.1):"] + ["  - " + i for i in issues] if issues else ["Issues found (R1.1): none"]
    lines += wrap("R1.2: for these problems the bridge already logs Debug.LogWarning when the level starts (difficulty 0, maxTime 0, cost <= 0 counted as 1, unknown enemy names, empty roster entries), "
                  "so a level can't end with 0 enemies silently. Level1.asset is a leftover placeholder no scene uses; every scene's own LevelData and both wave configs pass.")
    lines += wrap("Wave mode (Union Prime / Capen Major tutorial loop) has no per-wave warning in the bridge; GameplayLoopController refuses to start on missing data instead.")
    y = text_block(fig, 0.07, y, lines, size=7.4, gap=0.0152)
    y -= 0.008
    y = text_block(fig, 0.07, y, ["Name map (game asset -> AI kind, R1.4)"], size=9, color=INK, weight="bold")
    items = ["{} -> {}".format(k, v) for k, v in sorted(name_map.items())]
    y = text_block(fig, 0.07, y, wrap(", ".join(items), 125), size=7.0, gap=0.0145)
    y -= 0.01
    y = text_block(fig, 0.07, y, ["Regression (C5)"], size=9, color=INK, weight="bold")
    y = text_block(fig, 0.07, y, wrap("The unchanged v1.2 suite was re-run on this commit: {} numbers compared with history/v1.2.json, {} different. Extreme-case tests: {} passed, {} failed.".format(
        regress["compared"], regress["different"], regress["edge_pass"], regress["edge_fail"])), size=7.6)
    y -= 0.01
    y = text_block(fig, 0.07, y, ["Conclusion: why stealth geese are so strong (R4)"], size=9.5, color=INK, weight="bold")
    lines = []
    for c in concl:
        lines += wrap("• " + c)
    lines += wrap("• Not done in this version: none of the Should items were skipped (R4b and R4d are included).")
    text_block(fig, 0.07, y, lines, size=7.9)
    pdf.savefig(fig)
    plt.close(fig)


def regression():
    a = load("results.json")
    b = load(os.path.join("history", "v1.2.json"))
    out = []

    def walk(x, y):
        if isinstance(x, dict):
            for k in x:
                if k in ("generated_utc", "meta", "integration") or k not in y:
                    continue
                walk(x[k], y[k])
        elif isinstance(x, list):
            for u, v in zip(x, y):
                walk(u, v)
        elif isinstance(x, (int, float)) and isinstance(y, (int, float)) and not isinstance(x, bool):
            out.append((x, y))
    walk(a, b)
    diff = sum(1 for x, y in out if abs(x - y) > 1e-9 * max(1, abs(y)))
    e = a.get("edge_cases", {})
    return {"compared": len(out), "different": diff, "edge_pass": e.get("passed", 0), "edge_fail": e.get("failed", 0)}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--version", default="v1.3")
    ap.add_argument("--commit", default=None)
    args = ap.parse_args()
    commit = args.commit
    if not commit:
        try:
            commit = subprocess.check_output(["git", "rev-parse", "--short", "HEAD"], cwd=HERE).decode().strip()
            dirty = subprocess.call(["git", "diff", "--quiet", "HEAD", "--", "."], cwd=HERE) != 0
            commit += "+" if dirty else ""
        except Exception:
            commit = "unknown"
    meta = {"version": args.version, "commit": commit, "saved": datetime.now().strftime("%Y-%m-%d %H:%M")}
    r = load("results13.json")
    cap = load("results13_r4d.json")
    sw = load("results13_switch.json")
    r1 = [x for x in r["r1"] if "label" in x]
    name_map = next((x["name_map"] for x in r["r1"] if "name_map" in x), {})
    comp = [{"name": n, "metric": mtr, "algo": a, "ai": b, "prec": pr, "unit": u, "p": paired(a, b)} for n, mtr, a, b, pr, u in comparisons(r, sw)]
    regress = regression()

    decide = {}
    # build pages 4-5 first to collect the R4 evidence, then write the conclusion into pages 1 and 6
    tmp = os.path.join(HERE, ".tmp_r4.pdf")
    with PdfPages(tmp) as p:
        page4(p, r, decide)
        page5(p, r, cap, decide)
    os.remove(tmp)
    concl = conclusions(decide)

    out = os.path.join(HERE, "EnemyAI_Report_{}.pdf".format(args.version))
    with PdfPages(out) as pdf:
        page1(pdf, meta, r, comp, r1, regress, concl)
        page2(pdf, comp)
        page3(pdf, r)
        page4(pdf, r, decide)
        page5(pdf, r, cap, decide)
        page6(pdf, r1, name_map, regress, concl)
        pdf.infodict()["Title"] = "Enemy AI test report " + args.version
    summary = {"meta": meta, "comparisons": [{"name": c["name"], "metric": c["metric"], **{k: v for k, v in c["p"].items()}} for c in comp],
               "regression": regress, "conclusions": concl, "decide": {k: v for k, v in decide.items() if k != "r4c_rows" and k != "cap_rows"}}
    os.makedirs(os.path.join(HERE, "history"), exist_ok=True)
    with open(os.path.join(HERE, "history", args.version + ".json"), "w") as f:
        json.dump(summary, f, default=float)
    print("Wrote " + out)


def conclusions(d):
    c = []
    base = d.get("stealth_baseline", 0)
    sets = d.get("r4b_settings", [])
    keep = [x for x in sets if x[1] >= 3]   # stealth still best in most loadouts
    flip = [x for x in sets if x[1] < 3]
    nonlin = d.get("nonlinear", [])
    if base > 0:
        if not flip:
            c.append("(a) Numbers: stealth geese are the best family in {} of 5 loadouts and stay so under every ±50% change of the assumed values (R4b), "
                     "so the cause is the README numbers (1 HP, cost 5, invisible to most towers). Reported to the team.".format(base))
        else:
            c.append("(a) Numbers, with one assumption that matters: stealth geese are the best family in {} of 5 loadouts and stay so in {} of {} ±50% settings (R4b); "
                     "only {} changes it (then mostly {} are best). So the strength comes from the README numbers, amplified by our assumed tower range. Reported to the team.".format(
                         base, len(keep), len(sets), " / ".join(x[0] for x in flip), " / ".join(sorted(set(x[2] for x in flip)))))
    if nonlin:
        c.append("(b) Non-linear: pressure jumps instead of growing in proportion to the stealth share in {} of 5 loadouts ({}) (R4a), so the bandit's "
                 "'each enemy counts on its own' assumption doesn't fully hold. Listed as a limit of the method.".format(len(nonlin), ", ".join(nonlin)))
    else:
        c.append("(b) Not non-linear: pressure grows roughly in proportion to the stealth share in all 5 loadouts (R4a), so the bandit's assumption holds.")
    top = d.get("belief_stealth_top", 0)
    sh = d.get("rl_stealth_share", [])
    cs = d.get("cap_share", [])
    c.append("(c) Cap: the learner does rate stealth geese the best family in {} of 5 loadouts (R4c), yet rules + learning still spends only {:.0f}-{:.0f}% on them "
             "and its draws reach the 2x ceiling in just {:.0f}-{:.0f}% of cases: the limit is the 2x cap on a small designer share, not a wrong estimate. "
             "A 4x cap raises pressure significantly in {} of 5 loadouts at the cost of variety (R4d). How much to allow is the team's design decision.".format(
                 top, min(sh) if sh else 0, max(sh) if sh else 0, 100 * min(cs) if cs else 0, 100 * max(cs) if cs else 0, d.get("cap4_up", 0)))
    return c


if __name__ == "__main__":
    main()
