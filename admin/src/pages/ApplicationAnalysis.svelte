<script lang="ts">
	export let discordId: string;
	import Main from "../components/templates/Main.svelte";
	import Loader from "../components/misc/Loader.svelte";
	import request from "../lib/request";

	let data: any = undefined;
	let error: string | undefined;

	const loadAnalysis = async () => {
		try {
			const res = await request.get(`/applications/analysis/${discordId}`);
			data = res.data;
		} catch (e: any) {
			error = e?.message || "Failed to load analysis.";
		}
	};

	loadAnalysis();

	const formatGuildTypes = (types: any[]) =>
		types ? types.map(t => (t.emoji ?? "") + " " + (t.name ?? "")).join(", ") : "N/A";
</script>

<Main>
	<div class="row">
		<div class="col-12">
			<h3>Application Analysis</h3>
			<p class="text-muted">Discord ID: {discordId}</p>
		</div>

		{#if error}
			<div class="col-12">
				<div class="alert alert-danger">{error}</div>
			</div>
		{:else if data === undefined}
			<Loader />
		{:else}
			<div class="col-12 mb-4">
				<h5>Roblox User Analysis</h5>
				{#if data.robloxAnalysis}
					<p class="text-muted mb-1">Roblox User ID: {data.robloxUserId}</p>
					<table class="table table-bordered table-sm">
						<thead>
							<tr>
								<th>Field</th>
								<th>Value</th>
							</tr>
						</thead>
						<tbody>
							<tr>
								<td>Success</td>
								<td>{data.robloxAnalysis.success ?? "N/A"}</td>
							</tr>
							{#if data.robloxAnalysis.data}
								<tr>
									<td>Flag Type</td>
									<td>{data.robloxAnalysis.data.flagType ?? "N/A"}</td>
								</tr>
							{/if}
						</tbody>
					</table>
				{:else}
					<p class="text-muted">No Roblox data available (no verified Roblox user ID found).</p>
				{/if}
			</div>

			<div class="col-12">
				<h5>Discord User Analysis</h5>
				{#if data.discordAnalysis}
					<table class="table table-bordered table-sm mb-3">
						<thead>
							<tr>
								<th>Field</th>
								<th>Value</th>
							</tr>
						</thead>
						<tbody>
							<tr><td>User ID</td><td>{data.discordAnalysis.userId ?? "N/A"}</td></tr>
							<tr><td>Score Sum</td><td>{data.discordAnalysis.detail?.scoreSum ?? "N/A"}</td></tr>
							<tr><td>Appealing</td><td>{data.discordAnalysis.detail?.appealing ?? "N/A"}</td></tr>
							<tr><td>Past Offender</td><td>{data.discordAnalysis.detail?.pastOffender ?? "N/A"}</td></tr>
							<tr><td>Last Seen</td><td>{data.discordAnalysis.detail?.lastSeen ?? "N/A"}</td></tr>
						</tbody>
					</table>

					{#if data.discordAnalysis.guilds && data.discordAnalysis.guilds.length > 0}
						<h6>Flagged Guilds</h6>
						<table class="table table-bordered table-sm">
							<thead>
								<tr>
									<th>Guild ID</th>
									<th>Name</th>
									<th>Score</th>
									<th>First Seen</th>
									<th>Last Seen</th>
									<th>Types</th>
									<th>Messages</th>
									<th>Typing</th>
									<th>Interaction</th>
									<th>Indirect</th>
									<th>Staff</th>
									<th>Booster</th>
								</tr>
							</thead>
							<tbody>
								{#each data.discordAnalysis.guilds as guild}
									<tr>
										<td>{guild.id ?? "N/A"}</td>
										<td>{guild.name ?? "N/A"}</td>
										<td>{guild.score ?? 0}</td>
										<td>{guild.firstSeen ?? "N/A"}</td>
										<td>{guild.lastSeen ?? "N/A"}</td>
										<td>{formatGuildTypes(guild.types)}</td>
										<td>{guild.detail?.messages ?? 0}</td>
										<td>{guild.detail?.typing ?? 0}</td>
										<td>{guild.detail?.interaction ?? 0}</td>
										<td>{guild.detail?.indirect ?? 0}</td>
										<td>{guild.detail?.staff ?? false}</td>
										<td>{guild.detail?.booster ?? false}</td>
									</tr>
								{/each}
							</tbody>
						</table>
					{:else}
						<p class="text-muted">No flagged guilds.</p>
					{/if}
				{:else}
					<p class="text-muted">No Discord data available.</p>
				{/if}
			</div>
		{/if}
	</div>
</Main>
