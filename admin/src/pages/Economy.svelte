<script lang="ts">
	import { link } from "svelte-routing";
	import Main from "../components/templates/Main.svelte";
	import request from "../lib/request";
	import * as rank from "../stores/rank";
	import moment from "../lib/moment";

	let activeTab: "trade-web" | "mule-suspects" | "orphan-items" | "observatory" = "trade-web";


	type Node = { id: number; username: string };
	type Edge = { from: number; to: number; tradeId: number; createdAt: string };

	let tradeNodes: Node[] = [];
	let tradeEdges: Edge[] = [];
	let tradeLoading = false;
	let tradeError: string | undefined;

	const HUB_THRESHOLD = 3;
	const SVG_W = 800;
	const SVG_H = 600;

	function loadTradeGraph() {
		tradeLoading = true;
		tradeError = undefined;
		request.get("/economy/trade-graph?limit=200").then((d) => {
			tradeNodes = d.data.nodes;
			tradeEdges = d.data.edges;
		}).catch((e) => {
			tradeError = e.message;
		}).finally(() => {
			tradeLoading = false;
		});
	}

	function layoutNodes(nodes: Node[]): Map<number, { x: number; y: number }> {
		const positions = new Map<number, { x: number; y: number }>();
		const cx = SVG_W / 2;
		const cy = SVG_H / 2;
		const r = Math.min(cx, cy) - 60;
		nodes.forEach((n, i) => {
			const angle = (2 * Math.PI * i) / nodes.length;
			positions.set(n.id, {
				x: cx + r * Math.cos(angle),
				y: cy + r * Math.sin(angle),
			});
		});
		return positions;
	}

	function getOutDegree(nodeId: number): number {
		return tradeEdges.filter((e) => e.from === nodeId).length;
	}


	type MuleSuspect = {
		id: number;
		username: string;
		created_at: string;
		balance_robux: number;
		balance_tickets: number;
		limited_item_count: number;
	};

	let muleSuspects: MuleSuspect[] = [];
	let muleLoading = false;
	let muleError: string | undefined;

	function loadMuleSuspects() {
		muleLoading = true;
		muleError = undefined;
		request.get("/economy/mule-suspects").then((d) => {
			muleSuspects = d.data;
		}).catch((e) => {
			muleError = e.message;
		}).finally(() => {
			muleLoading = false;
		});
	}


	type OrphanItem = {
		user_asset_id: number;
		user_id: number;
		username: string;
		asset_id: number;
		asset_name: string;
		created_at: string;
	};

	let orphanItems: OrphanItem[] = [];
	let orphanLoading = false;
	let orphanError: string | undefined;

	function loadOrphanItems() {
		orphanLoading = true;
		orphanError = undefined;
		request.get("/economy/orphan-items").then((d) => {
			orphanItems = d.data;
		}).catch((e) => {
			orphanError = e.message;
		}).finally(() => {
			orphanLoading = false;
		});
	}


	const EXCHANGE_RATE = 10; // 1 RBX = 10 TIX

	type ObsUser = {
		id: number;
		username: string;
		status: number; // 1 = Ok, others = not-ok (banned/deleted/etc)
		balance_robux: number;
		balance_tickets: number;
		netWorth: number;
	};

	type ObsFilter = "all" | "active" | "banned";

	let obsRaw: ObsUser[] = [];
	let obsLoading = false;
	let obsError: string | undefined;
	let obsFilter: ObsFilter = "all";

	function loadObservatory() {
		obsLoading = true;
		obsError = undefined;
		request.get("/economy/observatory").then((d) => {
			obsRaw = (d.data as any[]).map((u) => ({
				...u,
				netWorth: (u.balance_robux || 0) + (u.balance_tickets || 0) / EXCHANGE_RATE,
			}));
		}).catch((e) => {
			obsError = e.message;
		}).finally(() => {
			obsLoading = false;
		});
	}

	function fmt(n: number): string {
		return Math.round(n).toLocaleString();
	}

	function calcGini(arr: number[]): number {
		if (arr.length === 0) return 0;
		const sorted = [...arr].sort((a, b) => a - b);
		const n = sorted.length;
		const sum = sorted.reduce((a, b) => a + b, 0);
		if (sum === 0) return 0;
		let sumOfRanks = 0;
		for (let i = 0; i < n; i++) sumOfRanks += (i + 1) * sorted[i];
		return Math.max(0, (2 * sumOfRanks) / (n * sum) - (n + 1) / n);
	}

	function calcMedian(arr: number[]): number {
		if (arr.length === 0) return 0;
		const sorted = [...arr].sort((a, b) => a - b);
		const mid = Math.floor(sorted.length / 2);
		return sorted.length % 2 !== 0 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
	}

	function exportCsv(users: ObsUser[], stats: any) {
		let csv = "PEKORA MACROECONOMIC EXPORT\n";
		csv += `Date Generated,${new Date().toLocaleString()}\n\n`;
		csv += "--- MACRO INDICATORS ---\n";
		csv += `Total Users Scanned,${stats.totalUsers}\n`;
		csv += `Total RBX,${stats.totalRBX}\n`;
		csv += `Total TIX,${stats.totalTIX}\n`;
		csv += `Total Net Worth (RBX),${stats.totalNW.toFixed(2)}\n`;
		csv += `Gini Coefficient,${stats.gini.toFixed(3)}\n`;
		csv += `Mean Net Worth,${stats.mean.toFixed(2)}\n`;
		csv += `Median Net Worth,${stats.median.toFixed(2)}\n`;
		csv += `Whale Dominance %,${stats.whalePct.toFixed(2)}%\n\n`;
		csv += "--- RAW USER DATA (Ranked by Net Worth) ---\n";
		csv += "Rank,Username,Status,RBX,TIX,Net Worth (RBX)\n";
		users.forEach((u, i) => {
			const safeName = u.username.replace(/,/g, "");
			const status = u.status === 1 ? "Active" : "Banned/Other";
			csv += `${i + 1},${safeName},${status},${u.balance_robux},${u.balance_tickets},${u.netWorth.toFixed(2)}\n`;
		});
		const blob = new Blob([csv], { type: "text/csv" });
		const url = URL.createObjectURL(blob);
		const a = document.createElement("a");
		a.href = url;
		a.download = `Pekora_Econ_Export_${Date.now()}.csv`;
		a.click();
		URL.revokeObjectURL(url);
	}

	let obsFiltered: ObsUser[] = [];
	$: obsFiltered = obsRaw.filter((u) => {
		if (obsFilter === "active") return u.status === 1;
		if (obsFilter === "banned") return u.status !== 1;
		return true;
	}).sort((a, b) => b.netWorth - a.netWorth);

	$: {
		const nws = obsFiltered.map((u) => u.netWorth);
		const totalRBX = obsFiltered.reduce((s, u) => s + (u.balance_robux || 0), 0);
		const totalTIX = obsFiltered.reduce((s, u) => s + (u.balance_tickets || 0), 0);
		const totalNW = obsFiltered.reduce((s, u) => s + u.netWorth, 0);
		const whaleNW = obsFiltered.length > 0 ? obsFiltered[0].netWorth : 0;
		const whalePct = totalNW > 0 ? (whaleNW / totalNW) * 100 : 0;
		const activeCount = obsFiltered.filter((u) => u.status === 1).length;
		const bannedCount = obsFiltered.filter((u) => u.status !== 1).length;
		const totalPop = activeCount + bannedCount;
		const pressure = totalPop > 0
			? ((activeCount * 1.0 - bannedCount * 1.5) / totalPop) * 0.05
			: 0;

		const low = obsFiltered.filter((u) => u.netWorth <= 100).length;
		const mid = obsFiltered.filter((u) => u.netWorth > 100 && u.netWorth <= 10000).length;
		const high = obsFiltered.filter((u) => u.netWorth > 10000).length;
		const lowPct  = totalPop > 0 ? (low / totalPop) * 100 : 33.3;
		const midPct  = totalPop > 0 ? (mid / totalPop) * 100 : 33.3;
		const highPct = totalPop > 0 ? (high / totalPop) * 100 : 33.3;

		const tixInRbx = totalTIX / EXCHANGE_RATE;
		const rbxCompPct = totalNW > 0 ? (totalRBX / totalNW) * 100 : 50;
		const tixCompPct = totalNW > 0 ? (tixInRbx / totalNW) * 100 : 50;
		const plebPct = 100 - whalePct;

		obsStats = {
			totalUsers: totalPop, totalRBX, totalTIX, totalNW,
			gini: calcGini(nws),
			mean: totalPop > 0 ? totalNW / totalPop : 0,
			median: calcMedian(nws),
			whalePct, plebPct, pressure,
			lowPct, midPct, highPct,
			rbxCompPct, tixCompPct,
			activeCount, bannedCount,
		};
	}

	let obsStats: any = {};


	function switchTab(tab: typeof activeTab) {
		activeTab = tab;
		if (tab === "trade-web"    && tradeNodes.length === 0 && !tradeLoading)   loadTradeGraph();
		if (tab === "mule-suspects" && muleSuspects.length === 0 && !muleLoading) loadMuleSuspects();
		if (tab === "orphan-items" && orphanItems.length === 0 && !orphanLoading) loadOrphanItems();
		if (tab === "observatory"  && obsRaw.length === 0 && !obsLoading)         loadObservatory();
	}

	loadTradeGraph();

	$: positions = layoutNodes(tradeNodes);
</script>

<svelte:head>
	<title>Economy</title>
</svelte:head>

<Main>
	{#if !rank.is("owner")}
		<div class="row">
			<div class="col-12">
				<p class="text-danger">You do not have permission to access this page.</p>
			</div>
		</div>
	{:else}
		<div class="row mb-3">
			<div class="col-12">
				<h2 class="mb-0">Economy</h2>
			</div>
		</div>

		<div class="row mb-3">
			<div class="col-12">
				<ul class="nav nav-tabs">
					<li class="nav-item">
						<button class={`nav-link${activeTab === "trade-web" ? " active" : ""}`} on:click={() => switchTab("trade-web")}>Trade Web</button>
					</li>
					<li class="nav-item">
						<button class={`nav-link${activeTab === "mule-suspects" ? " active" : ""}`} on:click={() => switchTab("mule-suspects")}>Mule Suspects</button>
					</li>
					<li class="nav-item">
						<button class={`nav-link${activeTab === "orphan-items" ? " active" : ""}`} on:click={() => switchTab("orphan-items")}>Orphan Items</button>
					</li>
					<li class="nav-item">
						<button class={`nav-link${activeTab === "observatory" ? " active" : ""}`} on:click={() => switchTab("observatory")}>Observatory</button>
					</li>
				</ul>
			</div>
		</div>

		{#if activeTab === "trade-web"}
			<div class="row mb-2">
				<div class="col-12 d-flex align-items-center gap-2">
					<h4 class="mb-0">Trade Web Visualization</h4>
					<button class="btn btn-sm btn-outline-secondary" on:click={loadTradeGraph} disabled={tradeLoading}>
						{tradeLoading ? "Loading…" : "Refresh"}
					</button>
				</div>
				<div class="col-12">
					<p class="text-muted mb-1">
						Nodes = players. Edges = completed trades. <span class="text-danger">Red nodes</span> are hubs (≥{HUB_THRESHOLD} outgoing trades).
					</p>
				</div>
			</div>
			{#if tradeError}
				<p class="text-danger">{tradeError}</p>
			{:else if tradeLoading}
				<div class="d-flex justify-content-center"><div class="spinner-border" /></div>
			{:else if tradeNodes.length === 0}
				<p>No completed trades found.</p>
			{:else}
				<div class="row">
					<div class="col-12" style="overflow-x: auto;">
						<svg width={SVG_W} height={SVG_H} style="background:#1a1a2e; border-radius:8px; display:block; margin:0 auto;">
							{#each tradeEdges as edge}
								{#if positions.has(edge.from) && positions.has(edge.to)}
									<line
										x1={positions.get(edge.from).x} y1={positions.get(edge.from).y}
										x2={positions.get(edge.to).x}   y2={positions.get(edge.to).y}
										stroke="#555" stroke-width="1" opacity="0.6"
									/>
								{/if}
							{/each}
							{#each tradeNodes as node}
								{#if positions.has(node.id)}
									{@const pos = positions.get(node.id)}
									{@const isHub = getOutDegree(node.id) >= HUB_THRESHOLD}
									<a href={`/admin/manage-user/${node.id}`} use:link>
										<circle cx={pos.x} cy={pos.y} r="14"
											fill={isHub ? "#dc3545" : "#0d6efd"}
											stroke={isHub ? "#ff6b6b" : "#3d8bfd"} stroke-width="2" />
										<text x={pos.x} y={pos.y + 26} text-anchor="middle"
											fill="white" font-size="10" font-family="monospace">{node.username}</text>
									</a>
								{/if}
							{/each}
						</svg>
					</div>
				</div>
				<div class="row mt-3">
					<div class="col-12">
						<h5>Hub Summary</h5>
						<table class="table table-dark table-sm table-bordered">
							<thead><tr><th>Username</th><th>User ID</th><th>Outgoing Trades</th></tr></thead>
							<tbody>
								{#each tradeNodes.filter(n => getOutDegree(n.id) >= HUB_THRESHOLD).sort((a, b) => getOutDegree(b.id) - getOutDegree(a.id)) as node}
									<tr>
										<td><a use:link href={`/admin/manage-user/${node.id}`}>{node.username}</a></td>
										<td>{node.id}</td>
										<td class="text-danger fw-bold">{getOutDegree(node.id)}</td>
									</tr>
								{/each}
								{#if tradeNodes.filter(n => getOutDegree(n.id) >= HUB_THRESHOLD).length === 0}
									<tr><td colspan="3">No hubs detected.</td></tr>
								{/if}
							</tbody>
						</table>
					</div>
				</div>
			{/if}

		<!-- ── Mule Suspects ──────────────────────────────────────────────────── -->
		{:else if activeTab === "mule-suspects"}
			<div class="row mb-2">
				<div class="col-12 d-flex align-items-center gap-2">
					<h4 class="mb-0">Mule Account Detection</h4>
					<button class="btn btn-sm btn-outline-secondary" on:click={loadMuleSuspects} disabled={muleLoading}>
						{muleLoading ? "Loading…" : "Refresh"}
					</button>
				</div>
				<div class="col-12">
					<p class="text-muted mb-1">Accounts less than 3 days old that hold significant wealth or limited items.</p>
				</div>
			</div>
			{#if muleError}
				<p class="text-danger">{muleError}</p>
			{:else if muleLoading}
				<div class="d-flex justify-content-center"><div class="spinner-border" /></div>
			{:else if muleSuspects.length === 0}
				<p>No mule suspects found.</p>
			{:else}
				<table class="table table-dark table-sm table-bordered table-hover">
					<thead><tr><th>Username</th><th>Account Age</th><th>Robux</th><th>Tix</th><th>Limiteds</th><th>Actions</th></tr></thead>
					<tbody>
						{#each muleSuspects as suspect}
							<tr>
								<td><a use:link href={`/admin/manage-user/${suspect.id}`}>{suspect.username}</a></td>
								<td>{moment(suspect.created_at).fromNow()}</td>
								<td class="text-success">{suspect.balance_robux.toLocaleString()}</td>
								<td class="text-warning">{suspect.balance_tickets.toLocaleString()}</td>
								<td>{suspect.limited_item_count}</td>
								<td><a use:link href={`/admin/manage-user/${suspect.id}`} class="btn btn-sm btn-outline-primary">Manage</a></td>
							</tr>
						{/each}
					</tbody>
				</table>
			{/if}

		<!-- ── Orphan Items ───────────────────────────────────────────────────── -->
		{:else if activeTab === "orphan-items"}
			<div class="row mb-2">
				<div class="col-12 d-flex align-items-center gap-2">
					<h4 class="mb-0">Orphan Item Tracker</h4>
					<button class="btn btn-sm btn-outline-secondary" on:click={loadOrphanItems} disabled={orphanLoading}>
						{orphanLoading ? "Loading…" : "Refresh"}
					</button>
				</div>
				<div class="col-12">
					<p class="text-muted mb-1">Limited items with no purchase transaction or trade record.</p>
				</div>
			</div>
			{#if orphanError}
				<p class="text-danger">{orphanError}</p>
			{:else if orphanLoading}
				<div class="d-flex justify-content-center"><div class="spinner-border" /></div>
			{:else if orphanItems.length === 0}
				<p>No orphan items found.</p>
			{:else}
				<table class="table table-dark table-sm table-bordered table-hover">
					<thead><tr><th>Item Name</th><th>Asset ID</th><th>User Asset ID</th><th>Current Owner</th><th>Created At</th><th>Actions</th></tr></thead>
					<tbody>
						{#each orphanItems as item}
							<tr>
								<td>{item.asset_name}</td>
								<td>{item.asset_id}</td>
								<td>{item.user_asset_id}</td>
								<td><a use:link href={`/admin/manage-user/${item.user_id}`}>{item.username}</a></td>
								<td>{moment(item.created_at).format("MMM DD YYYY, h:mm A")}</td>
								<td class="d-flex gap-1">
									<a use:link href={`/admin/asset/track?userAssetId=${item.user_asset_id}`} class="btn btn-sm btn-outline-info">Lineage</a>
									<a use:link href={`/admin/manage-user/${item.user_id}`} class="btn btn-sm btn-outline-primary">Owner</a>
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			{/if}

		<!-- ── Observatory ────────────────────────────────────────────────────── -->
		{:else if activeTab === "observatory"}
			{#if obsError}
				<p class="text-danger">{obsError}</p>
			{:else if obsLoading}
				<div class="d-flex justify-content-center"><div class="spinner-border" /></div>
			{:else if obsRaw.length === 0 && !obsLoading}
				<button class="btn btn-outline-secondary btn-sm" on:click={loadObservatory}>Load Data</button>
			{:else}
				<!-- Filter + actions -->
				<div class="row mb-3">
					<div class="col-12 d-flex align-items-center gap-3">
						<div class="d-flex gap-3">
							<div class="form-check form-check-inline">
								<input class="form-check-input" type="radio" id="obs-all" bind:group={obsFilter} value="all" />
								<label class="form-check-label" for="obs-all">All</label>
							</div>
							<div class="form-check form-check-inline">
								<input class="form-check-input" type="radio" id="obs-active" bind:group={obsFilter} value="active" />
								<label class="form-check-label" for="obs-active">Active</label>
							</div>
							<div class="form-check form-check-inline">
								<input class="form-check-input" type="radio" id="obs-banned" bind:group={obsFilter} value="banned" />
								<label class="form-check-label" for="obs-banned">Banned</label>
							</div>
						</div>
						<button class="btn btn-sm btn-outline-secondary ms-auto" on:click={() => exportCsv(obsFiltered, obsStats)}>Export CSV</button>
						<button class="btn btn-sm btn-outline-secondary" on:click={loadObservatory}>Refresh</button>
					</div>
				</div>

				<div class="row">
					<!-- Left column: Macro indicators -->
					<div class="col-12 col-md-4 mb-3">
						<div class="card">
							<div class="card-header">Macro Indicators</div>
							<div class="card-body p-0">
								<table class="table table-sm mb-0">
									<tbody>
										<tr><td class="text-muted">Total RBX</td><td class="text-success fw-bold">R${fmt(obsStats.totalRBX)}</td></tr>
										<tr><td class="text-muted">Total TIX</td><td class="text-warning fw-bold">T${fmt(obsStats.totalTIX)}</td></tr>
										<tr><td class="text-muted">Total Net Worth (RBX eq.)</td><td class="fw-bold">R${fmt(obsStats.totalNW)}</td></tr>
										<tr class="text-muted"><td>Conversion Rate</td><td>1 RBX = 10 TIX</td></tr>
										<tr title="0 = Perfect Equality, 1 = Maximum Inequality">
											<td class="text-muted">Gini Coefficient</td>
											<td class="fw-bold" style="color: {obsStats.gini > 0.6 ? '#dc3545' : obsStats.gini > 0.35 ? '#ffc107' : '#28a745'}">{obsStats.gini?.toFixed(3)}</td>
										</tr>
										<tr><td class="text-muted">Mean Net Worth</td><td class="text-success">R${fmt(obsStats.mean)}</td></tr>
										<tr><td class="text-muted">Median Net Worth</td><td class="text-success">R${fmt(obsStats.median)}</td></tr>
										<tr>
											<td class="text-muted">Econ-Pressure</td>
											<td style="color: {(obsStats.pressure || 0) >= 0 ? '#28a745' : '#dc3545'}">
												{(obsStats.pressure || 0) >= 0 ? "+" : ""}{((obsStats.pressure || 0) * 100).toFixed(2)}%
											</td>
										</tr>
									</tbody>
								</table>
							</div>
						</div>
						<div class="card mt-3">
							<div class="card-header">Population</div>
							<div class="card-body p-0">
								<table class="table table-sm mb-0">
									<tbody>
										<tr><td class="text-muted">Active</td><td class="text-success fw-bold">{obsStats.activeCount}</td></tr>
										<tr><td class="text-muted">Banned / Other</td><td class="text-danger fw-bold">{obsStats.bannedCount}</td></tr>
									</tbody>
								</table>
							</div>
						</div>
					</div>

					<!-- Right column: Charts -->
					<div class="col-12 col-md-8 mb-3">
						<div class="card mb-3">
							<div class="card-header">Visual Demographics</div>
							<div class="card-body">
								<p class="text-muted small mb-1">Class Distribution — Lower / Mid / Upper</p>
								<div class="obs-stacked-bar mb-1">
									<div class="obs-seg obs-seg-low"  style="width:{obsStats.lowPct?.toFixed(1)}%"  title="<100 RBX"></div>
									<div class="obs-seg obs-seg-mid"  style="width:{obsStats.midPct?.toFixed(1)}%"  title="100–10k RBX"></div>
									<div class="obs-seg obs-seg-high" style="width:{obsStats.highPct?.toFixed(1)}%" title=">10k RBX"></div>
								</div>
								<div class="obs-legend mb-3">
									<span><i class="obs-dot obs-seg-low"></i> &lt;100 R$</span>
									<span><i class="obs-dot obs-seg-mid"></i> 100–10k R$</span>
									<span><i class="obs-dot obs-seg-high"></i> &gt;10k R$</span>
								</div>

								<p class="text-muted small mb-1">Currency Composition (Total Value)</p>
								<div class="obs-stacked-bar mb-1">
									<div class="obs-seg obs-seg-rbx" style="width:{obsStats.rbxCompPct?.toFixed(1)}%">
										{(obsStats.rbxCompPct || 0) > 10 ? Math.round(obsStats.rbxCompPct) + "%" : ""}
									</div>
									<div class="obs-seg obs-seg-tix" style="width:{obsStats.tixCompPct?.toFixed(1)}%">
										{(obsStats.tixCompPct || 0) > 10 ? Math.round(obsStats.tixCompPct) + "%" : ""}
									</div>
								</div>
								<div class="obs-legend mb-3">
									<span><i class="obs-dot obs-seg-rbx"></i> RBX Value</span>
									<span><i class="obs-dot obs-seg-tix"></i> TIX Value</span>
								</div>

								<p class="text-muted small mb-1">Whale Dominance — Top #1 vs Rest</p>
								<div class="obs-stacked-bar mb-1">
									<div class="obs-seg obs-seg-whale" style="width:{obsStats.whalePct?.toFixed(1)}%">
										{(obsStats.whalePct || 0) > 10 ? Math.round(obsStats.whalePct) + "%" : ""}
									</div>
									<div class="obs-seg obs-seg-pleb" style="width:{obsStats.plebPct?.toFixed(1)}%"></div>
								</div>
								<div class="obs-legend">
									<span><i class="obs-dot obs-seg-whale"></i> #1 Richest</span>
									<span><i class="obs-dot obs-seg-pleb"></i> Everyone Else</span>
								</div>
							</div>
						</div>

						<div class="card">
							<div class="card-header">Top Wealthiest</div>
							<div class="card-body">
								{#if obsFiltered.length === 0}
									<p class="text-muted">No data for selected filter.</p>
								{:else}
									{#each obsFiltered.slice(0, 7) as user, i}
										{@const maxNW = obsFiltered[0]?.netWorth || 1}
										{@const pct = (user.netWorth / maxNW) * 100}
										<div class="mb-2">
											<div class="d-flex justify-content-between mb-1">
												<a use:link href={`/admin/manage-user/${user.id}`}>#{i + 1} {user.username}</a>
												<span class="text-muted small">R${fmt(user.netWorth)}</span>
											</div>
											<div class="progress" style="height: 10px;">
												<div class="progress-bar" style="width:{pct}%; background:{user.status !== 1 ? '#dc3545' : '#28a745'}"></div>
											</div>
										</div>
									{/each}
								{/if}
							</div>
						</div>
					</div>
				</div>
			{/if}
		{/if}
	{/if}
</Main>

<style>
	svg a {
		cursor: pointer;
	}

	/* ── Observatory Terminal ─────────────────────────────────────────────── */
	.obs-terminal {
		background: #0d0d0d;
		border: 1px solid #2a2a2a;
		border-radius: 6px;
		font-family: 'Courier New', Courier, monospace;
		font-size: 12px;
		overflow: hidden;
	}

	/* Stacked bars */
	.obs-stacked-bar {
		display: flex;
		height: 18px;
		border-radius: 3px;
		overflow: hidden;
		background: #1a1a1a;
		border: 1px solid #222;
	}
	.obs-seg {
		height: 100%;
		display: flex;
		align-items: center;
		justify-content: center;
		font-size: 10px;
		font-weight: bold;
		color: rgba(255,255,255,0.7);
		transition: width 0.4s ease;
		overflow: hidden;
	}
	.obs-seg-low   { background: #2d6a9f; }
	.obs-seg-mid   { background: #2ecc71; }
	.obs-seg-high  { background: #e74c3c; }
	.obs-seg-rbx   { background: #1e7e34; }
	.obs-seg-tix   { background: #856404; }
	.obs-seg-whale { background: #8e44ad; }
	.obs-seg-pleb  { background: #2c3e50; }

	.obs-legend {
		display: flex;
		gap: 12px;
		margin-top: 4px;
		flex-wrap: wrap;
	}
	.obs-legend span {
		font-size: 10px;
		color: #666;
		display: flex;
		align-items: center;
		gap: 4px;
	}
	.obs-dot {
		display: inline-block;
		width: 8px;
		height: 8px;
		border-radius: 2px;
		flex-shrink: 0;
	}

	/* Top users bar chart */
	.obs-bar-row {
		margin-bottom: 6px;
	}
	.obs-bar-label {
		display: flex;
		justify-content: space-between;
		align-items: baseline;
		margin-bottom: 2px;
	}
	.obs-user-link {
		color: #8ab4f8;
		text-decoration: none;
		font-size: 11px;
	}
	.obs-user-link:hover {
		text-decoration: underline;
		color: #a8c7fa;
	}
	.obs-bar-nw {
		color: #50c878;
		font-size: 10px;
	}
	.obs-bar-track {
		height: 10px;
		background: #1a1a1a;
		border-radius: 2px;
		overflow: hidden;
	}
	.obs-bar-fill {
		height: 100%;
		border-radius: 2px;
		transition: width 0.4s ease;
	}
	.obs-empty {
		color: #555;
		font-size: 11px;
		text-align: center;
		padding: 12px 0;
	}
</style>
