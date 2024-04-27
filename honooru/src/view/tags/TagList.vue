<template>
    <div>
        <app-menu></app-menu>


        <h2>
            tag search
        </h2>

        <div class="input-group">
            <input v-model="search" @keyup.enter="searchTags" class="form-control" />
            <button class="btn btn-primary" @click="searchTags">search</button>
        </div>

        <div v-if="searchResults.state == 'loaded'" class="no-underline-links">

            <a v-for="tag in tags" :style="{ color: '#' + tag.hexColor }" class="d-block" :href="'/tag/' + tag.id">
                <span :style="{ 'color': '#' + tag.hexColor }">
                    {{tag.name.split("_").join(" ")}}
                </span>

                <span class="" style="color: var(--bs-gray-600)">
                    {{tag.uses}}
                </span>
            </a>

        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { AppMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ApiError from "components/ApiError";
    import AlertCollapse from "components/AlertCollapse.vue";
    import PostSearch from "components/app/PostSearch.vue";
    import PostList from "components/app/PostList.vue";

    import { ExtendedTag, Tag, TagApi, TagSearchResults } from "api/TagApi";

    export const TagList = Vue.extend({
        props: {

        },

        data: function() {
            return {
                search: "" as string,

                searchResults: Loadable.idle() as Loading<TagSearchResults>
            }
        },

        methods: {
            searchTags: async function(): Promise<void> {
                this.searchResults = Loadable.loading();
                this.searchResults = await TagApi.search(this.search);
            }
        },

        computed: {

            tags: function(): ExtendedTag[] {
                if (this.searchResults.state != "loaded") {
                    return [];
                }

                return this.searchResults.data.tags;
            }


        },

        components: {
            InfoHover, ApiError, AlertCollapse,
            AppMenu,
            PostList,
            PostSearch,
        }

    });
    export default TagList;

</script>