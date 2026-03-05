<script lang="ts">
	import { link } from "svelte-routing";
	import Main from "../components/templates/Main.svelte";
	import request from "../lib/request";
	import * as rank from "../stores/rank";
	import moment from "../lib/moment";

	let activeTab: "trade-web" | "mule-suspects" | "orphan-items" = "trade-web";

	type Node = { id: number; username: string };
	type Edge = { from: number; to: number; tradeId: number; createdAt: string };

	let tradeNodes: Node[] = [];
	let tradeEdges: Edge[] = [];
	let tradeLoading = false;
	let tradeError: string | undefined;

	const HUB_THRESHOLD = 3; // outgoing edge count to flag as hub
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

	// Map node id → {x, y} positions on a circle
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


	function switchTab(tab: typeof activeTab) {
		activeTab = tab;
		if (tab === "trade-web" && tradeNodes.length === 0 && !tradeLoading) loadTradeGraph();
		if (tab === "mule-suspects" && muleSuspects.length === 0 && !muleLoading) loadMuleSuspects();
		if (tab === "orphan-items" && orphanItems.length === 0 && !orphanLoading) loadOrphanItems();
	}

	// Load the first tab on mount
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
						<button class={`nav-link${activeTab === "trade-web" ? " active" : ""}`} on:click={() => switchTab("trade-web")}>
							Trade Web
						</button>
					</li>
					<li class="nav-item">
						<button class={`nav-link${activeTab === "mule-suspects" ? " active" : ""}`} on:click={() => switchTab("mule-suspects")}>
							Mule Suspects
						</button>
					</li>
					<li class="nav-item">
						<button class={`nav-link${activeTab === "orphan-items" ? " active" : ""}`} on:click={() => switchTab("orphan-items")}>
							Orphan Items
						</button>
					</li>
				</ul>
			</div>
		</div>

		{#if activeTab === "trade-web"}
			<!-- ── Trade Web ─────────────────────────────────────────────── -->
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
							<!-- edges -->
							{#each tradeEdges as edge}
								{#if positions.has(edge.from) && positions.has(edge.to)}
									<line
										x1={positions.get(edge.from).x}
										y1={positions.get(edge.from).y}
										x2={positions.get(edge.to).x}
										y2={positions.get(edge.to).y}
										stroke="#555"
										stroke-width="1"
										opacity="0.6"
									/>
								{/if}
							{/each}
							<!-- nodes -->
							{#each tradeNodes as node}
								{#if positions.has(node.id)}
									{@const pos = positions.get(node.id)}
									{@const isHub = getOutDegree(node.id) >= HUB_THRESHOLD}
									<a href={`/admin/manage-user/${node.id}`} use:link>
										<circle
											cx={pos.x}
											cy={pos.y}
											r="14"
											fill={isHub ? "#dc3545" : "#0d6efd"}
											stroke={isHub ? "#ff6b6b" : "#3d8bfd"}
											stroke-width="2"
										/>
										<text
											x={pos.x}
											y={pos.y + 26}
											text-anchor="middle"
											fill="white"
											font-size="10"
											font-family="monospace"
										>{node.username}</text>
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
							<thead>
								<tr>
									<th>Username</th>
									<th>User ID</th>
									<th>Outgoing Trades</th>
								</tr>
							</thead>
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

		{:else if activeTab === "mule-suspects"}
			<!-- ── Mule Suspects ─────────────────────────────────────────── -->
			<div class="row mb-2">
				<div class="col-12 d-flex align-items-center gap-2">
					<h4 class="mb-0">Mule Account Detection</h4>
					<button class="btn btn-sm btn-outline-secondary" on:click={loadMuleSuspects} disabled={muleLoading}>
						{muleLoading ? "Loading…" : "Refresh"}
					</button>
				</div>
				<div class="col-12">
					<p class="text-muted mb-1">
						Accounts less than 3 days old that hold significant wealth or limited items. Typical mule pattern.
					</p>
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
					<thead>
						<tr>
							<th>Username</th>
							<th>Account Age</th>
							<th>Robux</th>
							<th>Tix</th>
							<th>Limiteds</th>
							<th>Actions</th>
						</tr>
					</thead>
					<tbody>
						{#each muleSuspects as suspect}
							<tr>
								<td><a use:link href={`/admin/manage-user/${suspect.id}`}>{suspect.username}</a></td>
								<td>{moment(suspect.created_at).fromNow()}</td>
								<td class="text-success">{suspect.balance_robux.toLocaleString()}</td>
								<td class="text-warning">{suspect.balance_tickets.toLocaleString()}</td>
								<td>{suspect.limited_item_count}</td>
								<td>
									<a use:link href={`/admin/manage-user/${suspect.id}`} class="btn btn-sm btn-outline-primary">Manage</a>
								</td>
							</tr>
						{/each}
					</tbody>
				</table>
			{/if}

		{:else if activeTab === "orphan-items"}
			<!-- ── Orphan Items ───────────────────────────────────────────── -->
			<div class="row mb-2">
				<div class="col-12 d-flex align-items-center gap-2">
					<h4 class="mb-0">Orphan Item Tracker</h4>
					<button class="btn btn-sm btn-outline-secondary" on:click={loadOrphanItems} disabled={orphanLoading}>
						{orphanLoading ? "Loading…" : "Refresh"}
					</button>
				</div>
				<div class="col-12">
					<p class="text-muted mb-1">
						Limited items with no purchase transaction or trade record — they appeared out of nowhere.
					</p>
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
					<thead>
						<tr>
							<th>Item Name</th>
							<th>Asset ID</th>
							<th>User Asset ID</th>
							<th>Current Owner</th>
							<th>Created At</th>
							<th>Actions</th>
						</tr>
					</thead>
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
		{/if}
	{/if}
</Main>

<style>
	svg a {
		cursor: pointer;
	}
</style>
