<script lang="ts">
    import Main from "../components/templates/Main.svelte";
    import request from "../lib/request";
    import * as rank from "../stores/rank";

    type WordEntry = { word: string; isHardcoded: boolean };

    let wordsInput = "";
    let addError = "";
    let addSuccess = "";
    let deleteError = "";

    let wordsPromise = loadWords();

    function loadWords() {
        return request.get<WordEntry[]>("/chat-filter").then(r => r.data);
    }

    async function addWords() {
        addError = "";
        addSuccess = "";
        if (!wordsInput.trim()) return;
        try {
            await request.post("/chat-filter", { words: wordsInput });
            addSuccess = "Words added successfully.";
            wordsInput = "";
            wordsPromise = loadWords();
        } catch (e: any) {
            addError = e?.response?.data?.message || "Failed to add words.";
        }
    }

    async function deleteWord(word: string) {
        deleteError = "";
        try {
            await request.delete(`/chat-filter?word=${encodeURIComponent(word)}`);
            wordsPromise = loadWords();
        } catch (e: any) {
            deleteError = e?.response?.data?.message || "Failed to delete word.";
        }
    }
</script>

<svelte:head>
    <title>Chat Filter</title>
</svelte:head>

<Main>
    {#if !rank.hasPermission("ManageChatFilter")}
        <div class="row">
            <div class="col-12">
                <p class="text-danger">You do not have permission to access this page.</p>
            </div>
        </div>
    {:else}
        <div class="row mb-3">
            <div class="col-12">
                <h2 class="mb-0">Chat Filter</h2>
                <p class="text-muted mt-1">Manage filtered words. Built-in words cannot be removed.</p>
            </div>
        </div>

        <div class="row mb-4">
            <div class="col-12 col-lg-6">
                <div class="card card-body">
                    <h5 class="mb-2">Add Words</h5>
                    <p class="text-muted small mb-2">Enter a single word or multiple words separated by commas.</p>
                    <div class="input-group">
                        <input
                            class="form-control"
                            type="text"
                            placeholder="e.g. word1, word2, word3"
                            bind:value={wordsInput}
                            on:keydown={(e) => e.key === "Enter" && addWords()}
                        />
                        <button class="btn btn-success" on:click={addWords}>Add</button>
                    </div>
                    {#if addError}
                        <p class="text-danger mt-2 mb-0">{addError}</p>
                    {/if}
                    {#if addSuccess}
                        <p class="text-success mt-2 mb-0">{addSuccess}</p>
                    {/if}
                </div>
            </div>
        </div>

        {#if deleteError}
            <div class="row mb-2">
                <div class="col-12">
                    <p class="text-danger">{deleteError}</p>
                </div>
            </div>
        {/if}

        {#await wordsPromise}
            <div class="d-flex justify-content-center"><div class="spinner-border" /></div>
        {:then words}
            <div class="row">
                <div class="col-12">
                    <table class="table table-dark table-sm table-bordered table-hover">
                        <thead>
                            <tr>
                                <th>Word</th>
                                <th>Type</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {#each words as entry}
                                <tr>
                                    <td>{entry.word}</td>
                                    <td>
                                        {#if entry.isHardcoded}
                                            <span class="badge bg-secondary">built-in</span>
                                        {:else}
                                            <span class="badge bg-primary">custom</span>
                                        {/if}
                                    </td>
                                    <td>
                                        {#if !entry.isHardcoded}
                                            <button
                                                class="btn btn-sm btn-outline-danger"
                                                on:click={() => deleteWord(entry.word)}
                                            >Delete</button>
                                        {/if}
                                    </td>
                                </tr>
                            {/each}
                        </tbody>
                    </table>
                </div>
            </div>
        {:catch err}
            <p class="text-danger">Failed to load words: {err?.message || "Unknown error"}</p>
        {/await}
    {/if}
</Main>
