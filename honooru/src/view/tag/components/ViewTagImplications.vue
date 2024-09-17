<template>
    <div>
        <div v-if="implications.state == 'idle'"></div>

        <div v-else-if="implications.state == 'loading'">
            loading...
        </div>

        <div v-else-if="implications.state == 'loaded'">
            <div class="mb-2">
                <h5 class="mb-1">
                    implies
                    <span class="text-muted h6">
                        - when this tag is added to a post, these tags will also be added
                    </span>
                </h5>
                <div v-for="i in tagImpliesExact" class="d-inline">
                    <div class="btn-group">
                        <button v-if="del == i.id" class="btn btn-danger" @click="deleteImplication(i.id)">
                            <span class="bi-trash"></span>
                        </button>

                        <a class="btn btn-info" :href="'/tag/' + i.id">
                            {{i.name}}
                        </a>

                        <button v-if="del != i.id" class="btn btn-info" @click="del = i.id">
                            &times;
                        </button>

                        <button v-else class="btn btn-secondary" @click="del = 0">
                            &times;
                        </button>
                    </div>

                    <span v-if="del == i.id" class="text-danger">
                        click again to delete
                    </span>
                </div>
                <div v-if="tagImpliesExact.length == 0">
                    <span class="text-muted">
                        nothing
                    </span>
                </div>

                <h5 class="mt-2 mb-1">
                    implies (chain)
                    <span class="text-muted h6">
                        - these implications are added by the exact implications above
                    </span>
                </h5>
                <div v-for="i in tagImpliesChain" class="d-inline">
                    <div class="btn-group">
                        <button v-if="del == i.id" class="btn btn-danger" @click="deleteImplication(i.id)">
                            <span class="bi-trash"></span>
                        </button>

                        <a class="btn btn-info" :href="'/tag/' + i.id">
                            {{i.name}}
                        </a>
                    </div>
                </div>
                <div v-if="tagImpliesChain.length == 0">
                    <span class="text-muted">
                        nothing
                    </span>
                </div>

                <div class="mt-2">
                    <div class="input-group">
                        <input class="form-control" type="text" v-model="newName" @keyup.enter="searchImplication" />
                        <span class="input-group-append">
                            <button class="btn btn-primary" @click="searchImplication">
                                search
                            </button>
                        </span>
                    </div>

                    <div v-if="search.state == 'idle'"></div>
                    <div v-else-if="search.state == 'loading'">
                        loading...
                    </div>

                    <div v-else-if="search.state == 'nocontent'" class="text-warning">
                        Tag {{newName}} does not exist!
                    </div>

                    <div v-else-if="search.state == 'loaded'">
                        found tag {{newName}}:
                        <span :style="{ 'color': '#' + search.data.hexColor }">
                            {{search.data.name}}
                        </span>
                        (id: {{search.data.id}})

                        <button class="btn btn-success" @click="createImplication">
                            insert
                        </button>
                    </div>
                </div>
            </div>

            <hr class="border border-secondary" />

            <div class="mb-2">
                <h5 class="mb-1">
                    implied by
                    <span class="text-muted h6">
                        - when these tags are added to a post, this tag is also added
                    </span>
                </h5>
                <div v-for="i in tagImpliedBy">
                    <a class="btn btn-secondary" :href="'/tag/' + i.id">
                        {{i.name}}
                    </a>
                </div>
                <div v-if="tagImpliedBy.length == 0">
                    <span class="text-muted">
                        nothing
                    </span>
                </div>
            </div>
        </div>

        <div v-else-if="implications.state == 'error'">
            <api-error :error="implications.problem"></api-error>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import ToggleButton from "components/ToggleButton";

    import { Tag, ExtendedTag, TagApi } from "api/TagApi";
    import { TagImplicationBlock, TagImplicationApi } from "api/TagImplicationApi";

    export const ViewTagImplications = Vue.extend({
        props: {
            TagId: { type: Number, required: true }
        },

        data: function() { 
            return {
                implications: Loadable.idle() as Loading<TagImplicationBlock>,

                del: 0 as number,
                create: 0 as number,

                newName: "" as string,
                search: Loadable.idle() as Loading<ExtendedTag>
            }
        },

        mounted: function(): void {
            this.getTagImplicationBlock(this.TagId);
        },

        methods: {
            getTagImplicationBlock: async function(tagID: number): Promise<void> {
                this.implications = Loadable.loading();
                this.implications = await TagImplicationApi.getBySourceTagID(tagID);
            },

            deleteImplication: async function(tagB: number): Promise<void> {
                console.log(`deleting implication between ${this.TagId} and ${tagB}`);
                const r: Loading<void> = await TagImplicationApi.delete(this.TagId, tagB);

                if (r.state == "loaded") {
                    Toaster.add("deleted implication", "successfully deleted implication", "success");
                    this.getTagImplicationBlock(this.TagId);
                } else if (r.state == "error") {
                    Toaster.add("failed!", `failed to delete implication: ${r.problem.detail}`, "danger");
                }
            },

            searchImplication: async function(): Promise<void> {
                this.search = Loadable.loading();
                console.log(`searching for tag with name of ${this.newName.trim()}`);
                this.search = await TagApi.getByName(this.newName.trim());
            },

            createImplication: async function(): Promise<void> {
                if (this.search.state != "loaded") {
                    console.error(`cannot create implication, searched tag does not exist`);
                    return;
                }

                const r: Loading<void> = await TagImplicationApi.insert(this.TagId, this.search.data.id);

                if (r.state == "loaded") {
                    Toaster.add("implication created", `successfully created an implication to ${this.search.data.name}`, "success");
                    this.getTagImplicationBlock(this.TagId);
                }

                if (r.state == "error") {
                    console.log(r.problem);
                    Toaster.add("failed!", `failed to create implication: ${r.problem.title}`, "danger");
                }
            }
        },

        computed: {
            tagImpliesChain: function(): Tag[] {
                if (this.implications.state != "loaded") {
                    return [];
                }

                const sources: number[] = this.implications.data.sources
                    .filter(iter => iter.tagA != this.TagId)
                    .map(iter => iter.tagB);
                return this.implications.data.tags.filter((iter: Tag) => {
                    return sources.indexOf(iter.id) > -1;
                });
            },

            tagImpliesExact: function(): Tag[] {
                if (this.implications.state != "loaded") {
                    return [];
                }

                const sources: number[] = this.implications.data.sources
                    .filter(iter => iter.tagA == this.TagId)
                    .map(iter => iter.tagB);

                return this.implications.data.tags.filter((iter: Tag) => {
                    return sources.indexOf(iter.id) > -1;
                });
            },

            tagImpliedBy: function(): Tag[] {
                if (this.implications.state != "loaded") {
                    return [];
                }

                const sources: number[] = this.implications.data.targets.map(iter => iter.tagA);
                return this.implications.data.tags.filter((iter: Tag) => {
                    return sources.indexOf(iter.id) > -1;
                });
            }

        },

        components: {

        },
    });

    export default ViewTagImplications;
</script>