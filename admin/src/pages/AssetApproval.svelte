<script>
	import Loader from "../components/misc/Loader.svelte";
	import Main from "../components/templates/Main.svelte";
	import request from "../lib/request";
	import { link } from "svelte-routing";
	let blur = 'false';
	let manuallyInsertUrl = '';

	let alreadyLoadedIds = [];
	function loadNewAssets(mode) {
		request.get(mode === 'group' ? `/groups/pending-icons` : `/${mode +'s'}/pending-assets`).then((res) => {
			if (!assetsToApprove) {
				assetsToApprove = [];
			}
			let newAssetsToApprove = res.data.filter((v) => {
				// icons and assets have id, group icons have name (always unique)
				v.unique_key = mode + ':' + (v.id || v.name);
				return !alreadyLoadedIds.includes(v.unique_key);
			}).map((v) => {
				v.mode = mode;
				alreadyLoadedIds.push(v.unique_key);
				return v;
			});
			if (newAssetsToApprove.length !== 0) {
				for (const item of newAssetsToApprove) {
					assetsToApprove.push(item);
				}
			}
			assetsToApprove = [...assetsToApprove];
		});
	}
	let modMode = 'default';

	let assetsToApprove = null;
	const loadAllTypes = () => {
		loadNewAssets('asset');
		loadNewAssets('icon');
		loadNewAssets('group');
	}

	$: {
		loadAllTypes();
	}

	function onClick(approve, is18Plus, del, asset) {
		return (e) => {
			assetsToApprove = assetsToApprove.filter((v) => v !== asset);
			if (assetsToApprove.length === 0) {
				loadAllTypes();
			}
			if (asset.mode === "asset") {
				request
					.post("/asset/moderate" + (del ? "-and-delete" : ""), {
						isApproved: approve,
						assetId: asset.id,
						is18Plus: is18Plus,
					})
					.then(() => {})
					.catch((e) => {
						console.error("[error] could not approve asset", e);
					});
			} else if (asset.mode === 'icon') {
				request
					.post("/icon/moderate", {
						isApproved: approve,
						iconId: asset.id,
						is18Plus: is18Plus,
					})
					.then(() => {})
					.catch((e) => {
						console.error("[error] could not approve asset", e);
					});
			}else if (asset.mode === 'group') {
				request.post("/groups/icon-toggle", {
					groupId: asset.group_id,
					name: asset.name,
					approved: approve ? 1 : 2,
				})
				.then(() => {
					console.log('group icon set');
				})
				.catch(e => {
					console.error('[error] could not modify group icon',e);
				})
				.finally(() => {
				})
			}else{
				console.error('invalid mode',asset.mode,asset);
			}
		};
	}

	$:{
		console.log('blur',blur);

	}

	let activeTab = 'pending';

	let reverseAssetId = '';
	let reverseAsset = null;
	let reverseLoading = false;
	let reverseError = '';

	let recentAssets = null;
	let recentLoading = false;

	$: if (activeTab === 'reverse' && recentAssets === null) {
		loadRecentModerations();
	}

	async function loadRecentModerations() {
		recentLoading = true;
		try {
			const logs = await request.get('/logs?logType=asset&limit=10&descending=true');
			const entries = (logs.data.data || []);
			const details = await Promise.all(
				entries.map((e) =>
					request.get('/asset/moderation-details?assetId=' + e.asset_id)
						.then((d) => ({ ...d.data, lastAction: e.action }))
						.catch(() => null)
				)
			);
			recentAssets = details.filter(Boolean);
		} catch {
			recentAssets = [];
		} finally {
			recentLoading = false;
		}
	}

	function lookupReverseAsset() {
		if (!reverseAssetId) return;
		let id = reverseAssetId.trim();
		const urlMatch = id.match(/\/([0-9]+)\//);
		if (urlMatch) id = urlMatch[1];
		else {
			const numMatch = id.match(/[0-9]+/);
			if (numMatch) id = numMatch[0];
		}
		reverseLoading = true;
		reverseError = '';
		reverseAsset = null;
		request.get('/asset/moderation-details?assetId=' + id)
			.then((d) => {
				d.data.mode = 'asset';
				reverseAsset = d.data;
			})
			.catch(() => {
				reverseError = 'Asset not found or you do not have permission.';
			})
			.finally(() => {
				reverseLoading = false;
			});
	}

	function reverseDecision(approve, del = false) {
		if (!reverseAsset) return;
		request.post('/asset/moderate' + (del ? '-and-delete' : ''), {
			assetId: reverseAsset.id,
			isApproved: approve,
			is18Plus: false,
		})
		.then(() => {
			reverseAsset = null;
			reverseAssetId = '';
		})
		.catch((e) => {
			reverseError = e?.response?.data?.message || 'Failed to update moderation status.';
		});
	}

	function reverseRecentDecision(asset, approve, del = false) {
		request.post('/asset/moderate' + (del ? '-and-delete' : ''), {
			assetId: asset.id,
			isApproved: approve,
			is18Plus: false,
		})
		.then(() => {
			recentAssets = recentAssets.filter((a) => a.id !== asset.id);
		})
		.catch((e) => {
			console.error('[error] could not reverse asset decision', e);
		});
	}
</script>


<style>
	.btn-group button {
		width: 100%;
	}
	input {
		width: 100%;
	}
	.nav-link {
		width: auto;
		background: none;
		border: none;
		color: #e0e0e0;
	}
	.nav-link.active {
		color: #212529;
	}
</style>


<svelte:head>
	<title>Asset Approval</title>
</svelte:head>

<Main>
	<div class="row">
		<div class="col-12">
			<h1>Asset Approval</h1>
			<ul class="nav nav-tabs mb-3">
				<li class="nav-item">
					<button class={"nav-link" + (activeTab === 'pending' ? ' active' : '')} on:click={() => activeTab = 'pending'}>Pending</button>
				</li>
				<li class="nav-item">
					<button class={"nav-link" + (activeTab === 'reverse' ? ' active' : '')} on:click={() => activeTab = 'reverse'}>Reverse Decision</button>
				</li>
			</ul>
		</div>

		{#if activeTab === 'pending'}
		<div class="col-12">
			<div class="row">
				<div class="col-6 col-lg-4">
					<select class="form-control" bind:value={modMode}>
						<option value="default">Striped BG</option>
						<option value="white">White BG</option>
						<option value="black">Black BG</option>
					</select>
				</div>
				<div class="col-6 col-lg-3">
					<select class="form-control" bind:value={blur}>
						<option value="false">Disable Blur</option>
						<option value="true">Enable Blur</option>
					</select>
				</div>
			</div>
				<div class="row">
					<div class="col-10 col-lg-4">
						<label for="manual-insert">Manually Insert Asset Into Queue</label>
						<input
							type="text"
							class="form-control dark-input"
							id="manual-insert"
							placeholder="Item URL or Asset ID"
							bind:value={manuallyInsertUrl}
						/>
					</div>
					<div class="col-2">
						<button class="btn btn-primary mt-4" on:click={(e) => {
							if (manuallyInsertUrl) {
								let asset = manuallyInsertUrl.match(/\/[0-9]+\//);
								if (!asset || !asset[0]) {
									asset = manuallyInsertUrl.match(/[0-9]+/);
								} else {
									asset[0] = asset[0].slice(1, -1);
								}
								if (asset[0]) {
									let id = asset[0];
									request.get("/asset/moderation-details?assetId=" + id).then((d) => {
										console.log(d.data);
										if (!assetsToApprove) {
											assetsToApprove = [];
										}
										d.data.mode = 'asset';
										assetsToApprove.unshift(d.data);
										assetsToApprove = [...assetsToApprove];
									});
								}
							}
						}}>Insert</button>
					</div>
				</div>
		</div>
		<div class={"col-12 mt-4"}>
			{#if assetsToApprove === null}
				<Loader />
			{:else if assetsToApprove.length === 0}
				<p class="text-cener">There are no assets to approve at this time.</p>
			{:else}
				<div class={"row mb-4"}>
					{#each assetsToApprove as asset}
						<div class="col-12 mt-4 mb-4">
							<div class={"card card-body mod-card-" + modMode}>
								<div class="row">
									<div class="col-12 col-lg-6">
										<div class="mod-icon">
											<h3 class="text-left text-info">{asset.group_id ? ('Group ' + asset.group_id) : asset.name}</h3>
											<p class="text-left text-info">By <a use:link href={`/admin/manage-user/${asset.creatorId || asset.user_id}`}>{asset.creatorName || asset.creatorname}</a></p>
											<a href={asset.group_id ? `/My/Groups.aspx?gid=${asset.group_id}` : `/catalog/${asset.asset_id || asset.id}/--`}>
												{#if asset.assetType === 'Audio'}
													<audio controls={true}>
														<source src={`/admin-api/api/assets/get-asset-stream?assetId=${asset.asset_id || asset.id}`} />
													</audio>
												{:else if asset.assetType === 'Video'}
													<video controls={true} width="400">
														<source src={`/admin-api/api/assets/get-asset-stream?assetId=${asset.asset_id || asset.id}`} type="video/mp4" />
														<track 
															kind="captions" 
															src="#" 
															srclang="en" 
															label="English" 
														/>
														Your browser does not support the video tag.
													</video>
												{:else}
													<img on:error={(e) => {
														console.log('[warn] image load failure',e);
														let assetId = asset.asset_Id || asset.id;
														console.log('asseTId',assetId);
													}} class={"d-block m-icon-image" + (blur==="true" ? " mod-blury-image" :"")} src={asset.group_id ? `${asset.name}` : `${asset.content_url}`} alt={`Asset ${asset.id || asset.group_id}`} />
												{/if}
											</a>
										</div>
									</div>
									<div class="col-12 col-lg-6">
										<div class="row">
											<div class="col-12">
												<div class="btn-group w-100">
													<button class="btn btn-success border border-dark" on:click={onClick(true, false, false, asset)}>
														OK
													</button>
												</div>
											</div>
											<div class="col-12 mt-4">
												<div class="btn-group w-100">
													<button class="btn btn-danger border border-dark" on:click={onClick(false, true, false, asset)}>
														BAD
													</button>
												
													<button class="btn btn-danger border border-dark" on:click={onClick(false, true, true, asset)}>
														BAD + DELETE
													</button>
												</div>
											</div>
											<div class="col-12 mt-4">
												<div class="btn-group w-100">
													{#if asset.assetType === 'Audio' || asset.assetType === 'Video' || asset.assetType === 'Model'}
														<button class="btn btn-warning border border-dark" on:click={() => window.open(`/admin-api/api/assets/get-asset-stream?assetId=${asset.asset_id || asset.id}`, '_blank')}>
															Download Asset
														</button>
													{/if}
												</div>
											</div>
										</div>
									</div>
								</div>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>
		{/if}

		{#if activeTab === 'reverse'}
		<div class="col-12">
			<h4>Reverse a Moderation Decision</h4>
			<div class="row mb-3">
				<div class="col-6 col-lg-4">
					<select class="form-control" bind:value={modMode}>
						<option value="default">Striped BG</option>
						<option value="white">White BG</option>
						<option value="black">Black BG</option>
					</select>
				</div>
				<div class="col-6 col-lg-3">
					<select class="form-control" bind:value={blur}>
						<option value="false">Disable Blur</option>
						<option value="true">Enable Blur</option>
					</select>
				</div>
			</div>
			<p class="text-muted">Enter an asset ID or URL to look it up and change its moderation status.</p>
			<div class="row">
				<div class="col-10 col-lg-4">
					<input
						type="text"
						class="form-control dark-input"
						placeholder="Asset ID or URL"
						bind:value={reverseAssetId}
						on:keydown={(e) => { if (e.key === 'Enter') lookupReverseAsset(); }}
					/>
				</div>
				<div class="col-2">
					<button class="btn btn-primary" on:click={lookupReverseAsset} disabled={reverseLoading}>
						{reverseLoading ? 'Loading...' : 'Lookup'}
					</button>
				</div>
			</div>

			{#if reverseError}
				<p class="text-danger mt-2">{reverseError}</p>
			{/if}

			{#if reverseAsset}
				<div class="row mt-4">
					<div class="col-12">
						<div class={"card card-body mod-card-" + modMode}>
							<div class="row">
								<div class="col-12 col-lg-6">
									<div class="mod-icon">
										<h3 class="text-left text-info">{reverseAsset.name}</h3>
										<p class="text-left text-info">By <a use:link href={`/admin/manage-user/${reverseAsset.creatorId}`}>{reverseAsset.creatorName}</a></p>
										<img class={"d-block m-icon-image" + (blur === "true" ? " mod-blury-image" : "")} src={reverseAsset.content_url} alt={`Asset ${reverseAsset.id}`} />
									</div>
								</div>
								<div class="col-12 col-lg-6">
									<div class="row">
										<div class="col-12">
											<div class="btn-group w-100">
												<button class="btn btn-success border border-dark" on:click={() => reverseDecision(true)}>
													Approve
												</button>
											</div>
										</div>
										<div class="col-12 mt-4">
											<div class="btn-group w-100">
												<button class="btn btn-danger border border-dark" on:click={() => reverseDecision(false)}>
													Decline
												</button>
												<button class="btn btn-danger border border-dark" on:click={() => reverseDecision(false, true)}>
													Decline + Delete
												</button>
											</div>
										</div>
									</div>
								</div>
							</div>
						</div>
					</div>
				</div>
			{/if}

			<h5 class="mt-4">Recent Moderation Actions</h5>
			{#if recentLoading}
				<Loader />
			{:else if recentAssets !== null && recentAssets.length === 0}
				<p class="text-muted">No recent moderation actions found.</p>
			{:else if recentAssets}
				<div class="row">
					{#each recentAssets as asset}
						<div class="col-12 mt-3">
							<div class={"card card-body mod-card-" + modMode}>
								<div class="row">
									<div class="col-12 col-lg-6">
										<div class="mod-icon">
											<h3 class="text-left text-info">{asset.name}</h3>
											<p class="text-left text-info">By <a use:link href={`/admin/manage-user/${asset.creatorId}`}>{asset.creatorName}</a></p>
											<img class={"d-block m-icon-image" + (blur === "true" ? " mod-blury-image" : "")} src={asset.content_url} alt={`Asset ${asset.id}`} />
										</div>
									</div>
									<div class="col-12 col-lg-6">
										<div class="row">
											{#if asset.lastAction === 'ReviewApproved'}
												<div class="col-12 mt-4">
													<div class="btn-group w-100">
														<button class="btn btn-danger border border-dark" on:click={() => reverseRecentDecision(asset, false, false)}>
															BAD
														</button>
														<button class="btn btn-danger border border-dark" on:click={() => reverseRecentDecision(asset, false, true)}>
															BAD + DELETE
														</button>
													</div>
												</div>
											{:else if asset.lastAction === 'Declined'}
												<div class="col-12">
													<div class="btn-group w-100">
														<button class="btn btn-success border border-dark" on:click={() => reverseRecentDecision(asset, true, false)}>
															OK
														</button>
													</div>
												</div>
											{/if}
										</div>
									</div>
								</div>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>
		{/if}
	</div>
</Main>
