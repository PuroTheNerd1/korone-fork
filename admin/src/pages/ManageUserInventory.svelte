<script lang="ts">
	import { navigate, link } from "svelte-routing";
	import Main from "../components/templates/Main.svelte";
	import request from "../lib/request";
	export let userId: string;
	let disabled = false;
	let errorMessage: string | undefined;
	import * as rank from "../stores/rank";
	rank.promise.then(() => {
		if (!rank.is("admin")) {
			errorMessage = "You must have the administrator permission to manage inventories.";
		}
	});
	let assetIdsToGive: { assetId: number; giveSerial: boolean; copies: number }[] = [];
	let userAssetsToRemove: { assetId: number; userAssetId: number; name: string }[] = [];
	let listOpen = false;
	let transferToUserId: string = "";
	let itemSearch: string = "";

	let removeItemsListOpen = false;
	let ownedUserAssets: { asset_id: number; name: string; user_asset_id: number }[] = null;
	let ownedUserItems: { asset_id: number; name: string; user_asset_id: number }[] = null;

	request.get("/user-collectibles?userId=" + userId).then((userassets) => {
		ownedUserAssets = userassets.data;
	});
	request.get("/user-items?userId=" + userId).then((items) => {
		ownedUserItems = items.data;
	});

	function isSelected(userAssetId: number): boolean {
		return userAssetsToRemove.some((v) => v.userAssetId === userAssetId);
	}

	function toggleItem(item: { asset_id: number; name: string; user_asset_id: number }) {
		const uaid = item.user_asset_id;
		if (isSelected(uaid)) {
			userAssetsToRemove = userAssetsToRemove.filter((v) => v.userAssetId !== uaid);
		} else {
			userAssetsToRemove = [{ userAssetId: uaid, name: item.name, assetId: item.asset_id }, ...userAssetsToRemove];
		}
	}

	$: filteredLimiteds = (ownedUserAssets ?? []).filter((item) =>
		item.name.toLowerCase().includes(itemSearch.toLowerCase())
	);
	$: filteredItems = (ownedUserItems ?? []).filter((item) =>
		item.name.toLowerCase().includes(itemSearch.toLowerCase())
	);
</script>

<style>
	p.err {
		color: red;
	}
	.group-header {
		text-align: center;
		font-weight: bold;
		font-size: 1.1rem;
		color: #aaa;
		letter-spacing: 0.1em;
		padding: 6px 0 4px;
	}
	.group-separator {
		border: none;
		border-top: 1px solid #444;
		margin: 10px 0;
	}
</style>

<svelte:head>
	<title>Manage Collectible Items</title>
</svelte:head>

<Main>
	<div class="row">
		<div class="col-12">
			{#if errorMessage}
				<p class="err">{errorMessage}</p>
			{/if}
		</div>
	</div>
	<div class="row">
		<div class="col-12">
			<h1>Remove Item</h1>
		</div>
		<div class="col-12">
			<button
				class="btn btn-outline-success"
				on:click={(e) => {
					e.preventDefault();
					removeItemsListOpen = !removeItemsListOpen;
				}}>{removeItemsListOpen ? "Close" : "Add Item to List"}</button
			>
			{#if removeItemsListOpen}
				<div class="mt-2">
					<input
						type="text"
						class="form-control dark-input mb-2"
						placeholder="Search items..."
						bind:value={itemSearch}
					/>
				</div>
				<div class="mt-1 row" style="max-height: 400px; overflow-y: auto;">
					{#if ownedUserAssets !== null}
						{#if filteredLimiteds.length > 0}
							<div class="col-12 group-header">LIMITEDS</div>
							{#each filteredLimiteds as item}
								<div
									class={`col-6 col-md-3 col-lg-2 ${isSelected(item.user_asset_id) ? "border" : ""}`}
									style="cursor: pointer;"
									on:click={() => toggleItem(item)}
								>
									<img style="width: 100%;" src={`/thumbs/asset.ashx?assetId=${item.asset_id}&width=420&height=420&format=png`} alt="Item" />
									<p class="text-truncate pb-0 mb-0">{item.name}</p>
									<p class="text-truncate mt-0">UAID #{item.user_asset_id}</p>
								</div>
							{/each}
						{/if}
						{#if filteredItems.length > 0}
							<div class="col-12"><hr class="group-separator" /></div>
							{#each filteredItems as item}
								<div
									class={`col-6 col-md-3 col-lg-2 ${isSelected(item.user_asset_id) ? "border" : ""}`}
									style="cursor: pointer;"
									on:click={() => toggleItem(item)}
								>
									<img style="width: 100%;" src={`/thumbs/asset.ashx?assetId=${item.asset_id}&width=420&height=420&format=png`} alt="Item" />
									<p class="text-truncate pb-0 mb-0">{item.name}</p>
									<p class="text-truncate mt-0">UAID #{item.user_asset_id}</p>
								</div>
							{/each}
						{/if}
						{#if filteredLimiteds.length === 0 && filteredItems.length === 0}
							<div class="col-12"><p>No items match your search.</p></div>
						{/if}
					{/if}
				</div>
			{/if}
		</div>
		<div class="col-12 mt-3">
			<h4>Items Being Taken</h4>
			{#if userAssetsToRemove.length === 0}
				<p>No items specified</p>
			{:else}
				{#each userAssetsToRemove as item}
					<p class="mt-0 mb-0">{item.name} - #{item.userAssetId}</p>
				{/each}
			{/if}
		</div>
		<div class="col-12">
			<hr />
		</div>
		<div class="col-12">
			<h1>Give Item</h1>
		</div>
		<div class="col-12">
			<button
				class="btn btn-outline-success"
				on:click={(e) => {
					e.preventDefault();
					listOpen = !listOpen;
				}}>{listOpen ? "Close" : "Add Item to List"}</button
			>
			{#if listOpen}
				<div class="form-control mt-2">
					<label for="asset-id">Asset ID</label>
					<input type="text" class="form-control dark-input" id="asset-id" {disabled} />
					<label for="copy-count">Copy Count</label>
					<input type="text" class="form-control dark-input" id="copy-count" value={1} {disabled} />
					<label for="give-serial">Give Serial</label>
					<select class="form-control" id="give-serial" {disabled}>
						<option value="false">No</option>
						<option value="true">Yes</option>
					</select>
					<button
						class="btn btn-outline-success mt-2"
						on:click={(e) => {
							e.preventDefault();
							assetIdsToGive = [
								{
									// @ts-ignore
									assetId: parseInt(document.getElementById("asset-id").value, 10),
									// @ts-ignore
									giveSerial: document.getElementById('give-serial').value === 'true' ? true : false,
									// @ts-ignore
									copies: parseInt(document.getElementById("copy-count").value, 10),
								},
								...assetIdsToGive,
							];
						}}>Add to List</button
					>
				</div>
			{/if}
		</div>
		<div class="col-12 mt-3">
			<h4>Items Being Given</h4>
			{#if assetIdsToGive.length === 0}
				<p>No items specified</p>
			{:else}
				{#each assetIdsToGive as item}
					<p class="mt-0 mb-0">{item.assetId} - x{item.copies}</p>
				{/each}
			{/if}
		</div>
		<div class="col-12">
			<hr />
		</div>
		<div class="col-12 mt-3">
			<div class="d-flex align-items-center gap-2">
				<label class="mb-0" for="transfer-to-user-id">Transfer items to</label>
				<input
					type="text"
					class="form-control dark-input"
					id="transfer-to-user-id"
					style="max-width: 200px;"
					placeholder="User ID"
					bind:value={transferToUserId}
					{disabled}
				/>
			</div>
		</div>
		{#if rank.hasPermission("TrackItem")}
		<div class="col-12">
			<hr />
		</div>
		<div class="col-12">
			<h1>Item Lineage</h1>
			<p class="text-muted">Click "View Lineage" on any limited item to see its full chain of custody.</p>
		</div>
		<div class="col-12">
			{#if ownedUserAssets === null}
				<p>Loading...</p>
			{:else if ownedUserAssets.length === 0}
				<p>No limited items.</p>
			{:else}
				<div class="row">
					{#each ownedUserAssets as item}
						<div class="col-6 col-md-3 col-lg-2 mb-3">
							<img style="width: 100%;" src={`/thumbs/asset.ashx?assetId=${item.asset_id}&width=420&height=420&format=png`} alt="Item" />
							<p class="text-truncate pb-0 mb-0">{item.name}</p>
							<p class="text-truncate mt-0 mb-1">UAID #{item.user_asset_id}</p>
							<a use:link href={`/admin/asset/track?userAssetId=${item.user_asset_id}`} class="btn btn-sm btn-outline-info w-100">View Lineage</a>
						</div>
					{/each}
				</div>
			{/if}
		</div>
		{/if}
		<div class="col-12 mt-4">
			<button
				class="btn btn-success"
				disabled={disabled || (assetIdsToGive.length === 0 && userAssetsToRemove.length === 0)}
				on:click={(e) => {
					e.preventDefault();
					if (disabled) {
						return;
					}
					let proms = [];
					for (const item of assetIdsToGive) {
						proms.push(
							request.post("/giveitem", {
								userId,
								assetId: item.assetId,
								copies: item.copies,
								giveSerial: item.giveSerial,
							})
						);
					}
					for (const item of userAssetsToRemove) {
						const trimmedTransfer = transferToUserId.trim();
						proms.push(request.post("/removeitem", {
							userId,
							userAssetId: item.userAssetId,
							...(trimmedTransfer !== "" ? { transferToUserId: parseInt(trimmedTransfer, 10) } : {}),
						}));
					}
					Promise.all(proms)
						.then(() => {
							navigate("/admin/manage-user/" + userId);
						})
						.finally(() => {
							disabled = false;
						})
						.catch((e) => {
							errorMessage = e.message;
						});
					disabled = true;
				}}>Submit Inventory Adjustment</button
			>
		</div>
	</div>
</Main>
