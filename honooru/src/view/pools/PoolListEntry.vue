
<template>
    <div>
        <h1>
            <span class="cursor-pointer" :class="[ opened == true ? 'bi-caret-down-fill' : 'bi-caret-right-fill' ]" title="Show posts in this pool" @click="togglePanel">
            </span> 

            <a :href="'/pool/' + pool.id">
                {{pool.name}}
            </a>
        </h1>

        <div v-if="loaded" v-show="opened" class="collapse hide" :id="'pool-images-' + pool.id">
            <h4>
                <span class="fw-normal">Created by</span>
                <user-account :account-id="pool.createdByID"></user-account>
                <span class="fw-normal">at</span>
                {{pool.timestamp | moment}}
            </h4>

            <post-list :q="'pool:' + pool.id" :limit="5" :hide-footer="true" :key="'pool-image-key-' + pool.id"></post-list>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import PostList from "components/app/PostList.vue";
    import UserAccount from "components/app/UserAccount.vue";

    import "filters/MomentFilter";

    import * as bootstrap from "bootstrap";

    import { PostPool, PostPoolApi } from "api/PostPoolApi";

    // by pulling this into a different component, we can prevent the <post-list> element from creating a network request
    // until the collapse is opened
    export const PoolListEntry = Vue.extend({
        props: {
            pool: { type: Object as PropType<PostPool>, required: true }
        },

        data: function() {
            return {
                // why is there both an opened and a loaded?
                // opened controls the v-show directive, while loaded controls when the <post-list> component is loaded
                // we only want to load the <post-list> component once, to prevent duplicate network requests and image thumbnail requests
                // so the first time the collapse is opened, we set loaded to true, so future collapse toggles will just hide/show the DOM,
                // instead of destroying and recycling the component
                opened: false as boolean,
                loaded: false as boolean,
            }
        },

        methods: {
            togglePanel: async function(): Promise<void> {
                // the first time this is opened, create the components by setting loaded to true,
                //      as opened will only control if the components are visible in the dom
                if (this.loaded == false) {
                    this.loaded = true;
                    await this.$nextTick();
                }

                if (this.opened == false) {
                    this.opened = true;
                    this.$nextTick(() => {
                        const collapse = bootstrap.Collapse.getOrCreateInstance(`#pool-images-${this.pool.id}`);
                        collapse.show();
                    });
                } else {
                    const collapse = bootstrap.Collapse.getOrCreateInstance(`#pool-images-${this.pool.id}`);
                    collapse.hide();
                    this.$nextTick(() => {
                        this.opened = false;
                    });
                }
            }
        },

        components: {
            PostList,
            UserAccount
        }

    });
    export default PoolListEntry;

</script>