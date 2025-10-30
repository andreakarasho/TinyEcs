use proc_macro::TokenStream;
use proc_macro2::Span;
use quote::quote;
use syn::{self, Ident};

pub(crate) fn derive_event_handler_macro(ast: &syn::DeriveInput) -> TokenStream {
    let name = &ast.ident;
    let handler_name = name.to_string().clone() + "Handler";
    let handler_ident = Ident::new(handler_name.as_str(), Span::call_site());

    let gen = quote! {
        pub struct #handler_ident<Marker, F> {
            callback: F,
            _marker: PhantomData<Marker>,
        }

        impl<Marker, F> #handler_ident<Marker, F> {
            pub fn from(callback: F) -> Self {
                Self {
                    callback: callback,
                    _marker: PhantomData,
                }
            }
        }

        impl<Marker, F> EntityCommand for #handler_ident<Marker, F>
        where
            Marker: Send + 'static,
            F: Send + IntoSystem<(), (), Marker> + 'static,
        {
            fn apply(self, id: Entity, world: &mut World) {
                let system_id = world.register_system(self.callback);
                world.entity_mut(id).insert(#name{
                    system_id,
                    active: false,
                });
            }
        }
    };
    gen.into()
}
