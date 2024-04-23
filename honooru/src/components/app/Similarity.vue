<template>
    <div>
        <h4 data-bs-toggle="collapse" data-bs-target="#similarity-settings">
            similar posts
            <span class="bi-gear"></span>
        </h4>

        <div id="similarity-settings" class="collapse">
            <div class="form-floating">
                <input v-model.number="options.minScore" type="number" class="form-control" />
                <label>minimum score</label>
            </div>
        </div>

        <div v-if="similarity.state == 'idle'"></div>

        <div v-else-if="similarity.state == 'loading'">
            loading...
        </div>

        <div v-else-if="similarity.state == 'loaded'">
            <div v-for="r in shownEntries" class="card" style="width: 180px;">
                <div v-if="r.post != null">

                    <div class="card-body">
                        <h5 class="card-title text-center">
                            <a :href="'/post/' + r.post.id" class="d-block">
                                <img :src="'/media/180x180/' + r.post.md5 + '.png'" class="card-img-top" width="180" style="max-height: 180px; object-fit: contain;" />
                                post #{{r.post.id}}
                            </a>

                            <span class="d-block">
                                {{r.score | locale(2)}}% similar
                            </span>

                            <span class="d-block">
                                {{r.post.width}}x{{r.post.height}}
                            </span>
                        </h5>
                    </div>
                </div>

                <div v-else-if="r.mediaAsset != null">
                    <div class="card-body">
                        <h5 class="card-title text-center">
                            <a :href="'/upload?m=' + r.mediaAsset.guid" class="d-block">
                                upload {{r.mediaAsset.guid}}
                            </a>

                            <span class="d-block">
                                {{r.score | locale(2)}}% similar
                            </span>
                        </h5>
                    </div>
                </div>
                
                <div v-else class="card-title text-danger text-center text-wrap">
                    missing md5 hash<br />
                    <code>{{r.postID}}</code>
                </div>
            </div>

            <div class="text-muted text-center">
                hiding {{hiddenCount}} entries

                <div>
                    <button v-if="options.showAll == true" class="btn btn-sm btn-link" @click="options.showAll = false">
                        hide
                    </button>

                    <button v-else class="btn btn-sm btn-link" @click="options.showAll = true">
                        show all
                    </button>
                </div>
            </div>

        </div>

        <div v-else-if="similarity.state == 'error'">
            <api-error :error="similarity.problem"></api-error>
        </div>

        <div v-else>
            unchecked state of similarity: {{similarity.state}}
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import ApiError from "components/ApiError";

    import { IqdbSearchResult, PostApi } from "api/PostApi";

    import "filters/LocaleFilter";

    export const Similarity = Vue.extend({
        props: {
            hash: { type: String, required: true },
            ExcludePostId: { type: Number, required: false },
            ExcludeMediaAssetId: { type: String, required: false }
        },

        data: function() {
            return {
                similarity: Loadable.idle() as Loading<IqdbSearchResult[]>,

                options: {
                    showAll: false as boolean,
                    minScore: 50 as number
                }
            }
        },

        mounted: function(): void {
            this.loadData();
        },

        methods: {
            loadData: async function(): Promise<void> {
                this.similarity = Loadable.loading();
                this.similarity = await PostApi.searchIqdb(this.hash);
            }
        },

        computed: {
            shownEntries: function(): IqdbSearchResult[] {
                if (this.similarity.state != "loaded") {
                    return [];
                }

                return this.similarity.data.filter(iter => {
                    return this.options.showAll == true
                        || iter.score >= this.options.minScore
                        && (iter.post == null || iter.post.id != this.ExcludePostId)
                        && (iter.mediaAsset == null || iter.mediaAsset.guid != this.ExcludeMediaAssetId)
                        ;
                });
            },

            hiddenCount: function(): number {
                if (this.similarity.state != "loaded") {
                    return -1;
                }

                return this.similarity.data.length - this.shownEntries.length;
            }

        },

        components: {
            ApiError
        }

    });

    export default Similarity;
</script>